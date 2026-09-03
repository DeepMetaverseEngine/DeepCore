using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GUI;

namespace DeepCore.Unity.OnGUI
{
    public class GUIBox : GUIObject
    {
        public GUIBox():base(new GUIStyle(UnityEngine.GUI.skin.box))
        {
        }
        protected override void Disposing()
        {
        }

        protected override void OnVisit(GUIGraphics g)
        {
            UnityEngine.GUI.Box(g.LocalBounds, Content, Style);
        }
        
    }
}
