using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;


namespace DeepCore.Unity3D_Android
{
    public class AndroidWWWHelper : Unity3D.Impl.UnityDriver.WWWHelper
    {
        private static AndroidJavaClass _helper;
        private static AndroidJavaClass helper
        {
            get
            {
                if (_helper != null) return _helper;
                _helper = new AndroidJavaClass("com.onegame.WWWHelper");
                using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    object jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
                    _helper.CallStatic("init", jo);
                }
                return helper;
            }
        }

        public override bool isFileExists(string path)
        {
            return helper.CallStatic<bool>("isFileExists", path);
        }

        public override byte[] getJavaData(string path)
        {
            byte[] imageByte = helper.CallStatic<byte[]>("getBytes", path);
            return imageByte;
        }

    }
}
