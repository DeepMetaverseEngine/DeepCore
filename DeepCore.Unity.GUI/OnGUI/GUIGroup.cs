using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GUI;

namespace DeepCore.Unity.OnGUI
{
    public class GUIClip : GUIContainer
    {

        public GUIClip() : base(new GUIStyle())
        {

        }
        protected override void OnVisit(GUIGraphics g)
        {
            MyGUI.BeginGroup(g.LocalBounds, Content, Style);
            try
            {
                base.OnVisit(g);
            }
            finally
            {
                UnityEngine.GUI.EndGroup();
            }
        }
    }
}
