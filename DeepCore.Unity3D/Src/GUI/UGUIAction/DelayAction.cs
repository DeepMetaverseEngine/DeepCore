using System;


namespace DeepCore.Unity3D.UGUIAction
{
    /// <summary>
    /// 延迟动作.
    ///  DelayAction action = new DelayAction();
    ///  action.Duration = 1;
    /// </summary>
    public partial class DelayAction : ActionBase
    {
        public const string ACTIONTYPE = "DelayAction";

        private float mTotalTime = 0.0f;
        private float mCurrentTime = 0.0f;

        public float Duration
        {
            get
            {
                return mTotalTime;
            }
            set
            {
                mTotalTime = value;
            }
        }

        public float CurrentTime
        {
            get
            {
                return mCurrentTime;
            }
        }

        public ActionFinishHandler UpdateCallBack { get; set; }
        public override void onUpdate(IActionCompment unit, float deltaTime)
        {
            if (deltaTime == 0 || (mCurrentTime == mTotalTime))
                return;

            float previousTime = mCurrentTime;
            float restTime = mTotalTime - mCurrentTime;
            mCurrentTime = Math.Min(mTotalTime, mCurrentTime + deltaTime);

            if (previousTime < mTotalTime && mCurrentTime >= mTotalTime)
            {
                mIsEnd = true;
            }
            if(UpdateCallBack != null)
            {
                UpdateCallBack.Invoke(unit);
            }

        }


        public override void onStart(IActionCompment unit)
        {
            if (mTotalTime == 0)
            {
                mIsEnd = true;
            }
        }

        public override bool IsEnd()
        {
            return mIsEnd;
        }

        public override string GetActionType()
        {
            return ACTIONTYPE;
        }
    }
}
