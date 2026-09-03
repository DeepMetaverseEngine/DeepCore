using System;
using UnityEngine.EventSystems;

namespace DeepCore.Unity3D.UGUI
{
    public partial class InteractiveInputField : UnityEngine.UI.InputField, IInputField
    {
        private DisplayNode mBinding;
        private PointerEventData mLastPointerDown;

        public UnityEngine.UI.Selectable AsSelectable { get { return this; } }
        public bool IsPressDown { get { return base.IsPressed(); } }
        public PointerEventData LastPointerDown { get { return mLastPointerDown; } }
        public Action<PointerEventData> event_PointerDown { get; set; }
        public Action<PointerEventData> event_PointerUp { get; set; }
        public Action<PointerEventData> event_PointerClick { get; set; }

        public Action<string> event_EndEdit { get; set; }
        public Action<string> event_ValueChanged { get; set; }

        public DisplayNode Binding
        {
            get { return mBinding; }
        }
        public string Text
        {
            get { return this.text; }
            set { this.text = value; }
        }

        public InteractiveInputField()
        {
            this.inputType = UnityEngine.UI.InputField.InputType.Standard;
            this.onEndEdit.AddListener(OnEndEdit);
            this.onValueChanged.AddListener(OnValueChanged);
        }
        protected override void Awake()
        {
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
            event_ValueChanged = null;
            event_EndEdit = null;
            this.onEndEdit.RemoveListener(OnEndEdit);
            this.onValueChanged.RemoveListener(OnValueChanged);
            base.OnDestroy();
        }

        protected virtual void OnValueChanged(string value)
        {
            if (event_ValueChanged != null)
                event_ValueChanged.Invoke(value);
        }
        protected virtual void OnEndEdit(string value)
        {
            if (event_EndEdit != null)
                event_EndEdit.Invoke(value);
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
        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            if (event_PointerClick != null) event_PointerClick(eventData);
        }
    }
    


}
