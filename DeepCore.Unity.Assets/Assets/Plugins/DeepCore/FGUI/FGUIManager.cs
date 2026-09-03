#if FGUI
using FairyGUI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepCore.Unity.FGUI
{
    public abstract class FGUIManager : Disposable
    {
        public static FGUIManager Instance { get; private set; }
        public FGUIView CurrentTopView => view_Stack.Count > 0 ? view_Stack.Last.Value : null;
        // 需要写个堆栈维护UI点击层级
        // 打开下一层枝需要关闭当前，并入栈，新的层关闭后，自动弹出上一层
        private LinkedList<FGUIView> view_Stack = new LinkedList<FGUIView>();
        public FGUIManager() { Instance = this; }
        protected override void Disposing()
        {

        }

        protected abstract FGUIView InnerCreateView(System.Type userClass);

        public V CreateView<V>(bool autoHideCurrentView) where V : FGUIView
        {
            var userClass = typeof(V);
            var newView = InnerCreateView(userClass) as FGUIView;
            if (newView != null)
            {
                if (autoHideCurrentView && CurrentTopView is FGUIView topView)
                {
                    topView.OnStackHide();
                }
                view_Stack.AddLast(newView);
                if (newView is V newV)
                {
                    return newV;
                }
                else
                {
                    throw new Exception($"打开的不是这个View：{userClass.FullName} - {newView.GetType().FullName}");
                }
            }
            else
            {
                Debug.LogError($"无法创建View：{userClass.FullName}");
                return null;
            }
        }
        internal void OnReleaseView(FGUIView fuck)
        {
            if (view_Stack.Remove(fuck))
            {

            }
            var currentTop = CurrentTopView;
            if (currentTop != null)
            {
                //弹出上一个
                currentTop.OnStackPop();
            }
        }
    }


    public class FGUIView : GComponent
    {
        public override void Dispose()
        {
            FGUIManager.Instance.OnReleaseView(this);
            base.Dispose();
        }
        protected internal virtual void OnStackPop()
        {
            if (!isDisposed)
                this.visible = true;
        }
        protected internal virtual void OnStackHide()
        {
            if (!isDisposed)
                this.visible = false;
        }
    }
}
#endif