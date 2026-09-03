using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepCore.Unity3D.UGUIAction
{

    /// <summary>
    /// 移动动作.
    /// eg:
    ///    MoveAction move = new MoveAction();
    ///    move.Duration = 1;
    ///    move.TargetX = 960;
    ///    move.TargetY = 640;
    ///    move.ActionEaseType = DeepCore.Unity3D.UGUIAction.EaseType.easeInOutBack;
    ///    move.ActionFinishCallBack = MoveActionCallBack;
    ///    cvs.AddAction(move);
    /// </summary>
    public partial class MoveAction : ActionBase
    {
        public const string ACTIONTYPE = "MoveAction";
        private float mTargetX = 0.0f;
        private float mTargetY = 0.0f;
        private float mStartX = 0.0f;
        private float mStartY = 0.0f;
        private float mTotalTime = 0.0f;
        private float mCurrentTime = 0.0f;
        private int mCurrentCycle = 0;
        private bool mReverse = false;
        private float mDeltaTime = 0.0f;
        private Vector2 mV = Vector2.zero;
        private bool bDelta = false;


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

        public float DeltaTime
        {
            get { return mDeltaTime; }
            set
            {
                mDeltaTime = value;
                bDelta = true;
            }
        }
        public override void onUpdate(IActionCompment unit, float deltaTime)
        {
            if (deltaTime == 0 || (mCurrentTime == mTotalTime)) return;

            float previousTime = mCurrentTime;
            float restTime = mTotalTime - mCurrentTime;
            float carryOverTime = deltaTime > restTime ? deltaTime - restTime : 0.0f;
            if (bDelta)
                mCurrentTime = Math.Min(mDeltaTime, mCurrentTime + deltaTime);
            else
                mCurrentTime = Math.Min(mTotalTime, mCurrentTime + deltaTime);
            if (mCurrentCycle < 0 && previousTime <= 0 && mCurrentTime > 0)
            {
                mCurrentCycle++;
            }
            if (previousTime < mDeltaTime && mCurrentTime >= mDeltaTime && bDelta) { bDelta = false; mCurrentTime = 0; }

            if (!bDelta)
            {
                float ratio = mCurrentTime / mTotalTime;
                bool reversed = mReverse && (mCurrentCycle % 2 == 1);

                float deltaX = mTargetX - mStartX;
                float deltaY = mTargetY - mStartY;
                float transitionValue = 0.0f;


                if (reversed == true)
                {
                    //transitionValue = Transitions.GetTransitionValue(mTransitions, (float)(1.0 - ratio));
                    transitionValue = EaseManager.EasingFromType(0, 1, (float)(1.0 - ratio), mEaseType);
                }
                else
                {
                    //transitionValue = Transitions.GetTransitionValue(mTransitions, ratio);
                    transitionValue = EaseManager.EasingFromType(0, 1, ratio, mEaseType);
                }

                float currentValueX = mStartX + transitionValue * deltaX;
                float currentValueY = mStartY + transitionValue * deltaY;

                mV.x = currentValueX;
                mV.y = currentValueY;

                unit.Position2D = mV;


                if (previousTime < mTotalTime && mCurrentTime >= mTotalTime) { mIsEnd = true; }

            }

        }

        public override void onStart(IActionCompment unit)
        {
            if (mTotalTime == 0)
            {
                unit.Position2D = new Vector2(TargetX, TargetY);
                mIsEnd = true;
            }
            else
            {
                Vector2 v = unit.Position2D;
                mStartX = v.x;
                mStartY = v.y;
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
