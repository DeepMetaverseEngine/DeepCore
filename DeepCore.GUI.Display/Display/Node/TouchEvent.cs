

using DeepCore.Geometry;

namespace DeepCore.GUI.Display.Node
{
    public class TouchEvent
    {
        public enum TouchEnum { eTouchBegin, eTouchEnd, eTouchMove, eLongTouch }
        private Vector2 mGlobalPos;
        private TouchEnum mTouchType;

        public Vector2 GlobalPos
        {
            set { mGlobalPos = value; }
            get { return mGlobalPos; }
        }
        public TouchEnum TouchType
        {
            set { mTouchType = value; }
            get { return mTouchType; }
        }
    }

    public class NodeTouch
    {
        private Vector2 mGlobalPos;
        private DisplayNode mTarget;
        private DisplayNode mCurrentTarget;
        private int mTouchIndex;

        public NodeTouch()
        {
            mTarget = null;
            mCurrentTarget = null;

            mGlobalPos =  Vector2.Zero;
            mTouchIndex = 0;
        }
        public Vector2 GlobalPos
        {
            set { mGlobalPos = value; }
            get { return mGlobalPos; }
        }

        public int TouchIndex
        {
            get { return mTouchIndex; }
        }

        public DisplayNode Target
        {
            get { return mTarget; }
            set { mTarget = value; }
        }

        public DisplayNode CurrentTarget
        {
            get { return mCurrentTarget; }
            set { mCurrentTarget = value; }
        }

        public NodeTouch Clone()
        {
            NodeTouch cloneData = new NodeTouch();
            cloneData.GlobalPos = this.GlobalPos;
            cloneData.Target = this.mTarget;
            cloneData.CurrentTarget = this.mCurrentTarget;
            cloneData.mTouchIndex = this.mTouchIndex;
            return cloneData;
        }

        public void SetData(Vector2 globalPos, DisplayNode target, DisplayNode currentTarget = null, int touchIndex = 0)
        {
            this.GlobalPos = globalPos;
            this.mTarget = target;
            this.mCurrentTarget = currentTarget;
            this.mTouchIndex = touchIndex;
        }

        public Vector2 GetLocalePoint()
        {
            return mTarget.GlobalToLocal(mGlobalPos).Value;
        }

        public Vector2 GetCurrentLocalePoint()
        {
            return mCurrentTarget.GlobalToLocal(mGlobalPos).Value;
        }
    }


}
