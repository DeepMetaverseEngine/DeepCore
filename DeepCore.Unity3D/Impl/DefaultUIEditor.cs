#if HZUI
using DeepCore.GUI.UI;
using DeepCore.Unity3D.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DeepCore.Unity3D.Src.Impl.UIEditor
{
    public class DefaultUIEditor : DeepCore.GUI.Editor.UIEditor
    {
        public static bool EnableG3Z = true;
        public static bool EnableM3Z = true;

        private readonly string EditorRoot;
        private readonly string EditorResRoot;
        private readonly string suffix_g3z;
        private readonly string suffix_m3z;

        public DefaultUIEditor(string uieditroot)
            : base(uieditroot + "/res/")
        {
            EditorRoot = uieditroot + "/";
            EditorResRoot = uieditroot + "/res/";
            UnityDriver.UnityInstance.RedirectImage = RedirectImage;
            if (UnityDriver.IsAndroid)
            {
                suffix_g3z = ".etc.g3z";
                suffix_m3z = ".etc.m3z";
            }
            else if (UnityDriver.IsIOS)
            {
                suffix_g3z = ".pvr.g3z";
                suffix_m3z = ".pvr.m3z";
            }
            else
            {
                suffix_g3z = ".etc.g3z";
                suffix_m3z = ".etc.m3z";
            }
        }

        public override UICompment CreateFromFile(string path)
        {
            return base.CreateFromFile(EditorRoot + path);
        }

        public string RedirectImage(string src)
        {
            if ((EnableG3Z || EnableM3Z) && (src.EndsWith(".png") || src.EndsWith(".jpg")))
            {
                string name = src.Substring(0, src.Length - 4);
                if (EnableG3Z)
                {
                    string name_g = name + suffix_g3z;
                    if (DeepCore.IO.Resource.ExistData(name_g))
                        return name_g;
                }
                if (EnableM3Z)
                {
                    string name_m = name + suffix_m3z;
                    if (DeepCore.IO.Resource.ExistData(name_m))
                        return name_m;
                }
            }
            return src;
        }


    }
}
#endif