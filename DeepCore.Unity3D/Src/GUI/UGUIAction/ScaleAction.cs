using System;
using UnityEngine;

namespace DeepCore.Unity3D.UGUIAction
{
    /// <summary>
    /// 缩放动作.
    /// eg:
    ///   ScaleAction scale = new ScaleAction();
    ///   scale.ScaleX = 1.5f;
    ///   scale.ScaleY = 1.5f;
    ///   scale.Duration = 1;
    ///   scale.ActionEaseType = DeepCore.Unity3D.UGUIAction.EaseType.easeInOutBack;
    ///   scale.ActionFinishCallBack = MoveActionCallBack;
    ///   cvs.AddAction(scale);
    /// </summary>

    public partial class ScaleAction : ActionBase
    {
        public enum ScaleTypes
        {
            Default,
            Center,
        }
        public const string ACTIONTYPE = "ScaleAction";
        private float mTargetX = 0.0f;
        private float mTargetY = 0.0f;
        private float mStartX = 0.0f;
        private float mStartY = 0.0f;
        private float mTotalTime = 0.0f;
        private float mCurrentTime = 0.0f;
        private int mCurrentCycle = 0;
        //private bool mReverse = false;
        private ScaleTypes mScaleType = ScaleTypes.Default;
        private Vector2 mDefaultPosition = Vector2.zero;
        private Vector2 mDefaultScale = Vector2.one;
        private Vector2 mV = Vector2.zero;
        private Vector2 mP = Vector2.zero;
        private int mPlayMode = 0;//播放模式 0播一次， 1往反一次，2无限往返
        public float ScaleX
        {
            get { return mTargetX; }
            set { mTargetX = value; }
        }

        public float ScaleY
        {
            get { return mTargetY; }
            set { mTargetY = value; }
        }

        public float Duration
        {
            get { return mTotalTime; }
            set { mTotalTime = value; }
        }
        public int PlayMode
        {
            get { return mPlayMode; }
            set { mPlayMode = value; }
        }

        public ScaleTypes ScaleType
        {
            get { return mScaleType; }
            set { mScaleType = value; }
        }

        public override void onUpdate(IActionCompment unit, float deltaTime)
        {
            if (deltaTime == 0 || (mCurrentTime == mTotalTime)) return;

            float previousTime = mCurrentTime;
            float restTime = mTotalTime - mCurrentTime;
            float carryOverTime = deltaTime > restTime ? deltaTime - restTime : 0.0f;

            mCurrentTime = Math.Min(mTotalTime, mCurrentTime + deltaTime);

            //if (mCurrentCycle < 0 && previousTime <= 0 && mCurrentTime > 0)
            //{
            //    mCurrentCycle++;
            //}

            float ratio = mCurrentTime / mTotalTime;
            //bool reversed = mReverse && (mCurrentCycle % 2 == 1);

            float deltaX = mTargetX - mStartX;
            float deltaY = mTargetY - mStartY;
            float transitionValue = 0.0f;


            if (mCurrentCycle % 2 == 1)
            {
                transitionValue = EaseManager.EasingFromType(0, 1, (float)(1.0 - ratio), mEaseType);
            }
            else
            {
                transitionValue = EaseManager.EasingFromType(0, 1, ratio, mEaseType);
            }

            float currentValueX = mStartX + transitionValue * deltaX;
            float currentValueY = mStartY + transitionValue * deltaY;

            mV.x = currentValueX;
            mV.y = currentValueY;
            unit.Scale = mV;

            if (mScaleType == ScaleTypes.Center)
            {
                mP.x = (unit.Size2D.x - unit.Size2D.x * currentValueX / mDefaultScale.x) / 2;
                mP.y = (unit.Size2D.y - unit.Size2D.y * currentValueY / mDefaultScale.y) / 2;
                unit.Position2D = mDefaultPosition + mP;
            }

            if (previousTime < mTotalTime && mCurrentTime >= mTotalTime)
            {
                if (mPlayMode == 2)
                {
                    mIsEnd = false;
                    mCurrentCycle++;
                    mCurrentTime = 0;
                }
                else if (mPlayMode == 1)
                {
                    mIsEnd = false;
                    mCurrentCycle++;
                    mCurrentTime = 0;
                    mPlayMode = 0;
                }
                else
                {
                    mIsEnd = true;
                }

            }
        }

        public override void onStart(IActionCompment unit)
        {
            if (mTotalTime == 0)
            {
                unit.Scale = new Vector2(mTargetX, mTargetY);
                if (ScaleType == ScaleTypes.Center)
                {
                    mP.x = (unit.Size2D.x - unit.Size2D.x * mTargetX / unit.Scale.x) / 2;
                    mP.y = (unit.Size2D.y - unit.Size2D.y * mTargetY / unit.Scale.y) / 2;
                    unit.Position2D = unit.Position2D + mP;
                }
                mIsEnd = true;
            }
            else
            {
                if (ScaleType == ScaleTypes.Center)
                {
                    mDefaultPosition = unit.Position2D;
                    mDefaultScale = unit.Scale;
                }
                Vector2 OldScale = unit.Scale;
                mStartX = OldScale.x;
                mStartY = OldScale.y;
            }
        }

        public override void onStop(IActionCompment unit, bool sendCallBack)
        {
            //if (ScaleType == ScaleTypes.Center)
            //    unit.Position2D = mDefaultPosition;
            base.onStop(unit, sendCallBack);
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
