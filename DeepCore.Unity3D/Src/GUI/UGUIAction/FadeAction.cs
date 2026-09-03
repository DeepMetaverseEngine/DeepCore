using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Unity3D.UGUIAction
{
    public partial class FadeAction : ActionBase
    {
        public const string ACTIONTYPE = "FadeAction";
        private float mTargetAlpha = 1.0f;
        private float mStartAlpha = 1.0f;
        private float mTotalTime = 0.0f;
        private float mCurrentTime = 0.0f;
        private int mCurrentCycle = 0;
        private bool mReverse = false;

        public float TargetAlpha
        {
            get { return mTargetAlpha; }
            set
            {
                value = Math.Min(Math.Max(0.0f, value), 1.0f);
                mTargetAlpha = value;
            }
        }

        public float Duration
        {
            get { return mTotalTime; }
            set { mTotalTime = value; }
        }

        public override void onUpdate(IActionCompment unit, float deltaTime)
        {
            if (deltaTime == 0 || (mCurrentTime == mTotalTime)) return;

            float previousTime = mCurrentTime;
            float restTime = mTotalTime - mCurrentTime;
            float carryOverTime = deltaTime > restTime ? deltaTime - restTime : 0.0f;

            mCurrentTime = Math.Min(mTotalTime, mCurrentTime + deltaTime);

            if (mCurrentCycle < 0 && previousTime <= 0 && mCurrentTime > 0)
            {
                mCurrentCycle++;
            }

            float ratio = mCurrentTime / mTotalTime;
            bool reversed = mReverse && (mCurrentCycle % 2 == 1);

            float deltaX = mTargetAlpha - mStartAlpha;

            float transitionValue = 0.0f;


            if (reversed == true)
            {
                transitionValue = EaseManager.EasingFromType(0, 1, (float)(1.0 - ratio), mEaseType);
            }
            else
            {
                transitionValue = EaseManager.EasingFromType(0, 1, ratio, mEaseType);
            }

            float currentValue = mStartAlpha + transitionValue * deltaX;


            unit.Alpha = currentValue;


            if (previousTime < mTotalTime && mCurrentTime >= mTotalTime) { mIsEnd = true; }

        }

        public override void onStart(IActionCompment unit)
        {
            if (mTotalTime == 0)
            {
                unit.Alpha = mTargetAlpha;
                mIsEnd = true;
            }
            else
            {
                mStartAlpha = unit.Alpha;
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
