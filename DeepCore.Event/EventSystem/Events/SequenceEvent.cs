using DeepCore.GameEvent;

namespace DeepCore.Event.EventSystem.Events
{
    [Event("一个个顺序执行，直到全部结束", "Async")]
    public class SequenceEvent : BaseEvent
    {
        private BaseEvent mLastRunningEvent;
        protected override bool OnTryStartChild(BaseEvent e)
        {
            if (State != EventState.Running)
            {
                return false;
            }
            if (mLastRunningEvent == null)
            {
                mLastRunningEvent = e;
                return true;
            }
            if (mLastRunningEvent.IsStoped)
            {
                if (!mLastRunningEvent.IsSuccessed)
                {
                    Stop(false, "!mLastRunningEvent.IsSuccessed");
                    return false;
                }
                mLastRunningEvent = e;
                return true;
            }
            return false;
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
            if (FrameIndex > 0 && IsChildrenStoped)
            {
                Stop(IsChildrenSuccess);
            }
        }

    }
}
