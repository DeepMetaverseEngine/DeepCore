using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GUI;

namespace DeepCore.Unity.OnGUI
{
    public class GUIButton : GUIObject
    {
        public GUIButton() : base(new GUIStyle(UnityEngine.GUI.skin.button))
        {
        }
        protected override void Disposing()
        {
            Click = null;
        }
        protected override void OnVisit(GUIGraphics g)
        {
            if (UnityEngine.GUI.Button(g.LocalBounds, Content, Style))
            {
                Input.ResetInputAxes();
                Click?.Invoke(this);
            }
        }
        public event Action<GUIButton> Click;
    }
}
