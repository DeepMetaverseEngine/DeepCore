using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Unity3D.UGUIAction
{
    public partial class ActionBase : IAction
    {
        public delegate void ActionFinishHandler(IActionCompment sender);
        public ActionFinishHandler ActionFinishCallBack;
        protected bool mIsEnd = false;
        protected EaseType mEaseType = EaseType.linear;

        public EaseType ActionEaseType
        {
            get { return mEaseType; }
            set { mEaseType = value; }
        }

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
