
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;

namespace DeepCore.GUI.Display.Action
{
    public class MoveAction : ActionBase
    {
        public const string ACTIONTYPE = "MoveAction";
        private float mTargetX = 0.0f;
        private float mTargetY = 0.0f;
        private int mTransitions = Transitions.LINEAR;
        private float mStartX = 0.0f;
        private float mStartY = 0.0f;
        private float mTotalTime = 0.0f;
        private float mCurrentTime = 0.0f;
        private int mCurrentCycle = 0;
        private bool mReverse = false;

        public int TransitionsType
        {
            get { return mTransitions; }
            set { mTransitions = value; }
        }

        public float TargetX
        {
            get { return mTargetX; }
            set { mTargetX = value; }
        }

        public float TargetY
        {
            get { return mTargetY; }
            set { mTargetY = value; }
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

            float deltaX = mTargetX - mStartX;
            float deltaY = mTargetY - mStartY;
            float transitionValue = 0.0f;
          
      
            if (reversed == true)
            {
                transitionValue = Transitions.GetTransitionValue(mTransitions, (float)(1.0 - ratio));
            }
            else
            {
                transitionValue = Transitions.GetTransitionValue(mTransitions, ratio);
            }

            float currentValueX = mStartX + transitionValue * deltaX;
            float currentValueY = mStartY + transitionValue * deltaY;

            unit.X = currentValueX;
            unit.Y = currentValueY;

            if (previousTime < mTotalTime && mCurrentTime >= mTotalTime) { mIsEnd = true; }

        }

        public override void onStart(IActionCompment unit)
        {
            mStartX = unit.X;
            mStartY = unit.Y;
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

