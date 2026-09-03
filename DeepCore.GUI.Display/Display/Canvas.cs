using DeepCore.Geometry;

namespace DeepCore.GUI.Display
{
    //	------------------------------------------------------------------------------------------
    //	-by zhangyifei
    //	------------------------------------------------------------------------------------------

    public abstract class Canvas
    {
        public readonly Vector2 Pointer = new Vector2();

        public abstract void OnUpdate();

        public abstract void OnPaint(Graphics g);

        virtual public bool OnPointerPressed(float x, float y) { return false; }

        virtual public bool OnPointerReleased(float x, float y) { return false; }

        virtual public bool OnPointerDragged(float x, float y) { return false; }

        virtual public bool IsHandlelByStage(float x,float y){return false;}
     
        abstract public void OnResumeApp();

        abstract public void OnPauseApp();

    }
}
