
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;

namespace DeepCore.GUI.Display.Action
{
  public class ActionBase : IAction
    {
        public delegate void ActionFinishHandler(IActionCompment sender);
        public ActionFinishHandler ActionFinishCallBack;
        protected bool mIsEnd = false;

        public virtual void onUpdate(IActionCompment unit, float deltaTime)
        {
          
        }

        public virtual void onStart(IActionCompment unit)
        {
           
        }

        public virtual void onStop(IActionCompment unit, bool sendCallBack)
        {
            if (sendCallBack == true && ActionFinishCallBack != null) { ActionFinishCallBack(unit); }
            if (ActionFinishCallBack != null) { ActionFinishCallBack = null; }
        }

        public virtual bool IsEnd()
        {
            return mIsEnd;
        }

        public virtual string GetActionType()
        {
            throw new NotImplementedException();
        }
    }
}

