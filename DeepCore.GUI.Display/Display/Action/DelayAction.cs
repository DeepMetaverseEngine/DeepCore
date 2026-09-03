
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;

namespace DeepCore.GUI.Display.Action
{
   public class DelayAction : ActionBase
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

      public override void onUpdate(IActionCompment unit, float deltaTime)
      {
          if(deltaTime == 0 || (mCurrentTime == mTotalTime))
              return;

          float previousTime = mCurrentTime;
          float restTime = mTotalTime - mCurrentTime;
          mCurrentTime = Math.Min(mTotalTime, mCurrentTime + deltaTime);

          if(previousTime < mTotalTime && mCurrentTime >= mTotalTime)
          {
              mIsEnd = true;
          }

      }


      public override void onStart(IActionCompment unit)
      {
       
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
