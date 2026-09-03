using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GUI;

namespace DeepCore.Unity.OnGUI
{
    public class GUILabel : GUIObject
    {
        public GUILabel() : base(new GUIStyle(UnityEngine.GUI.skin.label))
        {
        }
        protected override void Disposing()
        {
        }

        protected override void OnVisit(GUIGraphics g)
        {
            UnityEngine.GUI.Label(g.LocalBounds, Content, Style);
        }

    }
}
