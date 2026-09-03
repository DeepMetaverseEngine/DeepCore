using DeepCore.GameEvent;

namespace DeepCore.Event.EventSystem.Events
{
    [Event("并发事件，事件以所有事件结束作为结束，结果为所有子事件全部成功", "Async")]
    public class ParallelEvent : BaseEvent
    {
        protected override void OnChildStop(BaseEvent e)
        {
            base.OnChildStop(e);
            if (FrameIndex > 0 && IsChildrenStoped)
            {
                Stop(SuccessChildCount == ChildCount);
            }
        }

        protected override void OnFirstUpdate(int ms)
        {
            base.OnFirstUpdate(ms);
            if (IsChildrenStoped)
            {
                Stop(SuccessChildCount == ChildCount);
            }
        }
    }
}
