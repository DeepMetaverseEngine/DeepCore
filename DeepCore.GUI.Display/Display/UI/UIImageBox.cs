using System;

namespace DeepCore.GUI.Display.UI
{
    public class UIImageBox : UIComponent
    {
        private bool mPlayAnimation = true;
        private int mAppointFrame = -1;

        private int mPlayTimes = -1;
        private int mCurrentPlayTimes;


        public delegate void PlayAnimationCallBack(UIImageBox sender);
        public event PlayAnimationCallBack OnAnimationCallBack;
        public event PlayAnimationCallBack OnAppointFrameCallBack;

        public UIImageBox()
        {
            this.Enable = false;
            this.EnableChildren = false;
            this.Layout.IsAutoPlay = false;
        }

        public void SetImg(int index)
        {
            if(this.Layout != null)
            {
                this.Layout.SetAtlasTile(index);
            }
        }

        public bool IsPlayAnimation()
        {
            return mPlayAnimation;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            if (mPlayAnimation && this.Layout != null)
            {
                PlayTimesTick();
            }
        }

        protected override void Disposing()
        {
            OnAnimationCallBack = null;
            base.Disposing();
        }

        /// <summary>
        /// 播放动画:anim动画名、times次数(-1=无限).
        /// </summary>
        /// <param name="anim"></param>
        /// <param name="times"></param>
        /// <param name="callBack"></param>
        public void PlayAnimate(int anim, int times, PlayAnimationCallBack callBack)
        {
            if(this.Layout != null)
            {
                this.Layout.PlayAnimate(anim);
            }
            else
            {
                return;
            }
            mCurrentPlayTimes = 0;
            mPlayAnimation = true;
            mPlayTimes = times;
            OnAnimationCallBack = callBack;
        }

        public void PlayAnimate(string animName, int times, PlayAnimationCallBack callBack)
        {
            if(this.Layout != null)
            {
                this.Layout.PlayAnimate(animName);
            }
            else
            {
                return;
            }

            mPlayAnimation = true;
            mPlayTimes = times;
            OnAnimationCallBack = callBack;
        }

        public void PlayAnimate(int anim, int times, PlayAnimationCallBack callBack, int appointFrameIndex, PlayAnimationCallBack appointFrameCallBack)
        {
            if(this.Layout != null)
            {
                this.Layout.PlayAnimate(anim);
            }
            else
            {
                return;
            }

            mPlayAnimation = true;
            mPlayTimes = times;
            OnAnimationCallBack = callBack;

            OnAppointFrameCallBack = appointFrameCallBack;
            mAppointFrame = appointFrameIndex;
        }

        public void StopAnimate(bool needCallBack)
        {
            if(this.Layout != null)
            {
                this.Layout.StopAnimate();
            }

            if(OnAnimationCallBack != null && needCallBack)
            {
                OnAnimationCallBack(this);
            }
            mPlayAnimation = false;
            OnAnimationCallBack = null;
            OnAppointFrameCallBack = null;
            mAppointFrame = -1;
        }

        private void PlayTimesTick()
        {
            if(mAppointFrame != -1)
            {
                if(mAppointFrame == this.Layout.GetCurrentFrame())
                {
                    if(OnAppointFrameCallBack != null)
                    {
                        OnAppointFrameCallBack.Invoke(this);
                    }
                }
            }

            if(mPlayTimes < 0)
            {
                return;
            }

            if(!this.Layout.IsEndFrame)
            {
                return;
            }

            mCurrentPlayTimes++;
            if(mCurrentPlayTimes >= mPlayTimes)
            {
                this.Layout.StopAnimate();
                mAppointFrame = -1;
                mPlayAnimation = false;
                if(OnAnimationCallBack != null)
                {
                    OnAnimationCallBack(this);
                }
                OnAnimationCallBack = null;
                OnAppointFrameCallBack = null;
            }
        }
    }
}

