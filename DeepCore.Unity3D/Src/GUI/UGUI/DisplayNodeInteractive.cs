using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.EventSystems;

namespace DeepCore.Unity3D.UGUI
{
    public interface IInteractiveComponent
    {
        DisplayNode Binding { get; }

        UnityEngine.UI.Selectable AsSelectable { get; }
        bool IsPressDown { get; }
        PointerEventData LastPointerDown { get; }
        Action<PointerEventData> event_PointerDown { get; set; }
        Action<PointerEventData> event_PointerUp { get; set; }
        Action<PointerEventData> event_PointerClick { get; set; }
        
    }

    public partial class DisplayNodeInteractive : UnityEngine.UI.Selectable, IPointerClickHandler, IInteractiveComponent
    {
        private PointerEventData mLastPointerDown;
        private DisplayNode mBinding;

        public UnityEngine.UI.Selectable AsSelectable { get { return this; } }
        public bool IsPressDown { get { return base.IsPressed(); } }
        public PointerEventData LastPointerDown { get { return mLastPointerDown; } }
        public Action<PointerEventData> event_PointerDown { get; set; }
        public Action<PointerEventData> event_PointerUp { get; set; }
        public Action<PointerEventData> event_PointerClick { get; set; }

        public DisplayNode Binding { get { return mBinding; } }

        public DisplayNodeInteractive()
        {
            //this.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        }

        protected override void Awake()
        {
            this.transition = UnityEngine.UI.Selectable.Transition.None;
            this.mBinding = DisplayNode.AsDisplayNode(gameObject);
            base.Awake();
        }

        protected override void OnDestroy()
        {
            mBinding = null;
            mLastPointerDown = null;
            event_PointerDown = null;
            event_PointerUp = null;
            event_PointerClick = null;
            base.OnDestroy();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            this.mLastPointerDown = eventData;
            base.OnPointerDown(eventData);
            if (IsPressed())
            {
                if (event_PointerDown != null) event_PointerDown(eventData);
            }
        }
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (!IsPressed())
            {
                if (event_PointerUp != null) event_PointerUp(eventData);
            }
        }
        
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (event_PointerClick != null) event_PointerClick(eventData);
        }


       
    }
}
