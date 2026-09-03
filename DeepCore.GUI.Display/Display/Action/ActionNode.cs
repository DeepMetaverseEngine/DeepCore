
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;
using System.Collections.Generic;

namespace DeepCore.GUI.Display.Action
{
    public class ActionNode : DisplayNode, IActionCompment
    {
        protected Dictionary<string, IAction> mMapAction = new Dictionary<string, IAction>();

        public ActionNode() : base("ActionNode")
        {

        }


        public new float X
        {
            get
            {
                return base.X;
            }
            set
            {
                base.X = value;
            }
        }
        public new float Y
        {
            get
            {
                return base.Y;
            }
            set
            {
                base.Y = value;
            }
        }
        public new float ScaleX
        {
            get
            {
                return base.ScaleX;
            }
            set
            {
                base.ScaleX = value;
            }
        }
        public new float ScaleY
        {
            get
            {
                return base.ScaleY;
            }
            set
            {
                base.ScaleY = value;
            }
        }
        public new float Alpha
        {
            get
            {
                return base.Alpha;
            }
            set
            {
                base.Alpha = value;
            }
        }

        protected virtual string ParseActionType(IAction action)
        {
            return action.GetActionType();
        }

        public virtual void AddAction(IAction action)
        {
            if(action == null)
            {
                throw new Exception("action can not be null");
            }
            string actionType = ParseActionType(action);

            IAction oldAction = null;
            if(HasAction(actionType))
            {
                mMapAction.TryGetValue(actionType, out oldAction);
                if(oldAction != null)
                {
                    RemoveAction(oldAction, false);
                }
            }

            StartAction(actionType, action);
        }

        protected virtual void StartAction(string actionType, IAction action)
        {
            mMapAction.Add(actionType, action);
            action.onStart(this);
        }

        public virtual void RemoveAction(IAction action, bool sendCallBack)
        {
            mMapAction.Remove(ParseActionType(action));
            action.onStop(this, sendCallBack);
        }

        public virtual void RemoveAction(string actionType, bool sendCallBack)
        {
            if(mMapAction == null || string.IsNullOrEmpty(actionType))
            {
                return;
            }
            IAction act = null;
            mMapAction.TryGetValue(actionType, out act);
            if(act != null)
            {
                act.onStop(this, sendCallBack);
            }
            mMapAction.Remove(actionType);
        }

        public virtual bool HasAction(IAction action)
        {
            string name = ParseActionType(action);
            return HasAction(name);
        }

        public virtual bool HasAction(string ActionType)
        {
            return mMapAction.ContainsKey(ActionType);
        }

        public virtual void RemoveAllAction(bool sendCallBack = false)
        {
            if(this.mMapAction != null)
            {
                List<IAction> removed = null;
                IAction act = null;
                foreach(KeyValuePair<string, IAction> kvp in this.mMapAction)
                {
                    act = kvp.Value;
                    if(removed == null)
                    {
                        removed = new List<IAction>();
                    }
                    removed.Add(act);
                }

                if(removed != null)
                {
                    foreach(IAction a in removed)
                    {
                        RemoveAction(a, sendCallBack);
                    }
                    removed = null;
                }
            }
        }

        public virtual void UpdateAction(float deltaTime)
        {
            if(this.mMapAction != null && this.mMapAction.Count > 0)
            {
                List<IAction> removed = null;
                IAction act = null;
                foreach(KeyValuePair<string, IAction> kvp in this.mMapAction)
                {
                    act = kvp.Value;
                    act.onUpdate(this, deltaTime);
                    if(act.IsEnd())
                    {
                        if(removed == null)
                        {
                            removed = new List<IAction>();
                        }
                        removed.Add(act);
                    }
                }

                if(removed != null)
                {
                    foreach(IAction a in removed)
                    {
                        RemoveAction(a, true);
                    }
                    removed = null;
                }
            }
        }

        public override void Update(float delatTime)
        {
            base.Update(delatTime);
            UpdateAction(delatTime);
        }

        protected override void Disposing()
        {
            RemoveAllAction();
            if(mMapAction != null)
            {
                mMapAction.Clear();
                mMapAction = null;
            }
            base.Disposing();
        }

    }
}

