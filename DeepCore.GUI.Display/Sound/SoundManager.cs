using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Sound
{
    public class SoundManager
    {
        private static SoundManager mInstance = null;

        protected SoundManager()
        {
            mInstance = this;
        }

        public static SoundManager GetInstance()
        {
            if (mInstance == null)
            {
                new SoundManager();
            }

            return mInstance;
        }

        public static void Dispose()
        {
            mInstance = null;
        }

        public virtual void PlaySound(string name) { }

        public virtual void Play3DSound(string name, float x, float y, float z) { }

        public virtual void PlaySoundByKey(string key) { }

        public virtual string GetDefaultBtnSound()
        {
            return null;
        }
    }
}
