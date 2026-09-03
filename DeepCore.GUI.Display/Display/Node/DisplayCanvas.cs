

using DeepCore.Log;
using System;

namespace DeepCore.GUI.Display.Node
{
    public class DisplayCanvas
    {
        private Logger log = LoggerFactory.GetLogger("DisplayCanvas");
        private DisplayStage mStage;
        private float mLastX = 0.0f;
        private float mLastY = 0.0f;
        public DisplayStage Stage { get => mStage; }

        public DisplayCanvas()
        {
            this.mStage = new DisplayStage();
            this.mStage.Position = Geometry.Vector3.Zero;
        }
        public void OnUpdate(float deltaTimeSec)
        {
            mStage.Update(deltaTimeSec);
        }
        public void OnPaint(Graphics g)
        {
            mStage.Visit(g);
        }
        public bool OnPointerDown(float x, float y)
        {
            TouchEvent touchData = new TouchEvent();
            touchData.GlobalPos = new Geometry.Vector2(x,y);
            touchData.TouchType = TouchEvent.TouchEnum.eTouchBegin;
            bool ret = mStage.SendTouchEvent(touchData);
            mLastX = x;
            mLastY = y;
            return ret;
        }
        public bool OnPointerUp(float x, float y)
        {
            TouchEvent touchData = new TouchEvent();
            touchData.GlobalPos = new Geometry.Vector2(x, y);
            touchData.TouchType = TouchEvent.TouchEnum.eTouchEnd;
            bool ret = mStage.SendTouchEvent(touchData);
            mLastX = x;
            mLastY = y;
            return ret;
        }
        public bool OnPointerMove(float x, float y, bool drag)
        {
            bool ret = false;
            if (drag)
            {
                if (mStage.IsInMove())
                {
                    TouchEvent touchData = new TouchEvent();
                    touchData.GlobalPos = new Geometry.Vector2(x, y);
                    touchData.TouchType = TouchEvent.TouchEnum.eTouchMove;
                    ret = mStage.SendTouchEvent(touchData);
                    mLastX = x;
                    mLastY = y;
                }
                else if (Math.Abs(x - mLastX) > 8 || Math.Abs(y - mLastY) > 8)
                {
                    TouchEvent touchData = new TouchEvent();
                    touchData.GlobalPos = new Geometry.Vector2(x, y);
                    touchData.TouchType = TouchEvent.TouchEnum.eTouchMove;
                    ret = mStage.SendTouchEvent(touchData);
                    mLastX = x;
                    mLastY = y;
                }
                else
                {
                    TouchEvent touchData = new TouchEvent();
                    touchData.GlobalPos = new Geometry.Vector2(x, y);
                    touchData.TouchType = TouchEvent.TouchEnum.eLongTouch;
                    ret = mStage.SendTouchEvent(touchData);
                    mLastX = x;
                    mLastY = y;
                }
            }
            return ret;
        }


    }
}
