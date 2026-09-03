
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;

namespace DeepCore.GUI.Display.Action
{
    

    /// <summary>
    /// Alpha变化动作.
    /// eg:
    ///    FadeAction action = new FadeAction();
    ///    action.TargetAlpha = 0;
    ///    action.Duration = 1;
    ///    action.ActionEaseType = CommonUnity3D.UGUIAction.EaseType.easeInOutBack;
    ///    action.ActionFinishCallBack = MoveActionCallBack;
    /// </summary>
   public class FadeAction :ActionBase
   {
       public const string ACTIONTYPE = "FadeAction";
       private float mTargetAlpha = 1.0f;
       private int mTransitions = Transitions.LINEAR;
       private float mStartAlpha = 1.0f;
       private float mTotalTime = 0.0f;
       private float mCurrentTime = 0.0f;
       private int mCurrentCycle = 0;
       private bool mReverse = false;

       public int TransitionsType
       {
           get { return mTransitions; }
           set { mTransitions = value; }
       }

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
               transitionValue = Transitions.GetTransitionValue(mTransitions, (float)(1.0 - ratio));
           }
           else
           {
               transitionValue = Transitions.GetTransitionValue(mTransitions, ratio);
           }

           float currentValue = mStartAlpha + transitionValue * deltaX;
     

           unit.Alpha= currentValue;
        

           if (previousTime < mTotalTime && mCurrentTime >= mTotalTime) { mIsEnd = true; }

       }

       public override void onStart(IActionCompment unit)
       {
           mStartAlpha = unit.Alpha;
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

