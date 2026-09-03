using System.Collections.Generic;
using DeepCore.GameEvent;

namespace DeepCore.Event.EventSystem.Events
{
    /// <summary>
    /// 一个个顺序执行，直到成功NeedSuccessCount个
    /// </summary>
    [Event("同时执行所有子事件，直到成功NeedSuccessCount个", "Async")]
    public class SelectorEvent : BaseEvent
    {
        [EventArgument("需要成功的数量，默认为1", 0)] public int SuccessCount = 1;
        [EventOutput("成功事件列表", 0)] public List<int> SuccessEventsID;

        public SelectorEvent(int count)
        {
            SuccessCount = count;
        }

        public SelectorEvent()
        {

        }

        protected override void OnFirstUpdate(int ms)
        {
            base.OnFirstUpdate(ms);
            if (IsChildrenStoped)
            {
                Stop(SuccessChildCount == ChildCount);
            }
        }

        protected override void OnChildStop(BaseEvent e)
        {
            base.OnChildStop(e);
            if (e.IsSuccessed)
            {
                if (SuccessEventsID == null)
                {
                    SuccessEventsID = new List<int>();
                }

                SuccessEventsID.Add(e.ID);
                if (SuccessEventsID.Count == SuccessCount)
                {
                    Stop(true);
                    return;
                }
            }

            if (FrameIndex > 0 && IsChildrenStoped)
            {
                Stop(false, "all stop");
            }
        }
    }
}