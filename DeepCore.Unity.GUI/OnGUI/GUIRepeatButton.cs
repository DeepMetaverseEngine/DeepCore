using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GUI;

namespace DeepCore.Unity.OnGUI
{
    public class GUIRepeatButton : GUIObject
    {
        public GUIRepeatButton() : base(new GUIStyle(UnityEngine.GUI.skin.button))
        {
        }
        protected override void Disposing()
        {
            Click = null;
        }
        protected override void OnVisit(GUIGraphics g)
        {
            if (UnityEngine.GUI.RepeatButton(g.LocalBounds, Content, Style))
            {
                Input.ResetInputAxes();
                Click?.Invoke(this);
            }
        }
        public event Action<GUIRepeatButton> Click;
    }
}
