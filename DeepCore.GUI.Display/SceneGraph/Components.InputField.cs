namespace DeepCore.GUI.SceneGraph
{

    public class InputComponent : DisplayNodeComponent
    {
        public InputComponent()
        {
        }
        protected override void OnDispose(DisplayNode owner)
        {
            this.CleanEvents();
        }

        protected override void OnAdded()
        {
            base.OnAdded();
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
        }
        //----------------------------------------------------------------------------------------------------------

        protected virtual void CleanEvents()
        {
            KeyDown = null;
            KeyUp = null;
            KeyPress = null;
        }

        public delegate void KeyHandler(InputComponent sender, KeyboardArgs args);
        public event KeyHandler KeyDown;
        public event KeyHandler KeyUp;
        public event KeyHandler KeyPress;


        //----------------------------------------------------------------------------------------------------------
        internal void Canvas_KeyDown(KeyboardArgs obj)
        {
            if (Enable) KeyDown?.Invoke(this, obj);
        }
        internal void Canvas_KeyUp(KeyboardArgs obj)
        {
            if (Enable) KeyUp?.Invoke(this, obj);
        }
        internal void Canvas_KeyPress(KeyboardArgs obj)
        {
            if (Enable) KeyPress?.Invoke(this, obj);
        }
    }

}
