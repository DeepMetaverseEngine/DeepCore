using DeepCore.Reflection;
using System.Collections.Generic;

namespace DeepCore.EventTrigger.Data
{
    //-------------------------------------------------------------------------------------
    [Desc("事件开关改变", "[基础]/触发器事件")]
    public class EventActiveChangedInvoke : AbstractTrigger
    {
        [EventIDAttribute]
        [Desc("触发事件名字")]
        public string EventName;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.ActiveChanged()", EventName);
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            EventExecutor trigger = api.Group.GetEditEvent(EventName);
            if (trigger != null)
            {
                api.listen_EventActiveChanged(args, trigger);
            }
            else
            {
                api.listen_EventActiveChanged(args, api);
            }
        }

        [TriggingArg("IsActive")] public bool IsActive(IEventArguments args) => args.TriggingBoolValue;

    }
    
    [Desc("Main", "[基础]/触发器事件")]
    public class EventMainInvoke : AbstractTrigger
    {
        [EventIDAttribute]
        [Desc("触发事件名字")]
        public string EventName;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.Main()", EventName);
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            EventExecutor trigger = api.Group.GetEditEvent(EventName);
            if (trigger != null)
            {
                api.listen_EventActionMain(args, trigger);
            }
            else
            {
                api.listen_EventActionMain(args, api);
            }
        }
    }

    [Desc("Over", "[基础]/触发器事件")]
    public class EventOverInvoke : AbstractTrigger
    {
        [EventIDAttribute]
        [Desc("触发事件名字")]
        public string EventName;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.Over()", EventName);
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            EventExecutor trigger = api.Group.GetEditEvent(EventName);
            if (trigger != null)
            {
                api.listen_EventActionOver(args, trigger);
            }
            else
            {
                api.listen_EventActionOver(args, api);
            }
        }
    }

    [Desc("触发器将执行", "[基础]/触发器事件")]
    public class EventBeginInvoke : AbstractTrigger
    {
        [EventIDAttribute]
        [Desc("触发事件名字")]
        public string EventName;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}将执行", EventName);
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            EventExecutor trigger = api.Group.GetEditEvent(EventName);
            if (trigger != null)
            {
                //trigger.OnActionBegin += api.onEventActionBegin;
                api.listen_EventActionBegin(args, trigger);
            }
            else
            {
                api.listen_EventActionBegin(args, api);
            }
        }
    }

    [Desc("触发器已执行", "[基础]/触发器事件")]
    public class EventEndInvoke : AbstractTrigger
    {
        [EventIDAttribute]
        [Desc("触发事件名字")]
        public string EventName;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}已执行", EventName);
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            EventExecutor trigger = api.Group.GetEditEvent(EventName);
            if (trigger != null)
            {
                //trigger.OnActionEnd += api.onEventActionEnd;
                api.listen_EventActionEnd(args, trigger);
            }
            else
            {
                api.listen_EventActionEnd(args, api);
            }
        }
    }

    //-------------------------------------------------------------------------------------

    [Desc("This.事件开关改变", "[基础]/触发器事件")]
    public class ThisEventActiveChangedInvoke : AbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("ActiveChanged()");
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            api.listen_EventActiveChanged(args, api);
        }

        [TriggingArg("IsActive")] public bool IsActive(IEventArguments args) => args.TriggingBoolValue;

    }

    [Desc("this.Main", "[基础]/触发器事件")]
    public class ThisEventMainInvoke : AbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("Main()");
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            api.listen_EventActionMain(args, api);
        }
    }

    [Desc("this.Over", "[基础]/触发器事件")]
    public class ThisEventOverInvoke : AbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("Over()");
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            api.listen_EventActionOver(args, api);
        }
    }

    [Desc("当前触发器将执行", "[基础]/触发器事件")]
    public class ThisEventBeginInvoke : AbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当前将执行");
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            api.listen_EventActionBegin(args, api);
        }
    }

    [Desc("当前触发器已执行", "[基础]/触发器事件")]
    public class ThisEventEndInvoke : AbstractTrigger
    {
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("当前已执行");
        }
        protected override void Listen(EventExecutor api, IEventArguments args)
        {
            api.listen_EventActionEnd(args, api);
        }
    }
    //-------------------------------------------------------------------------------------
}
