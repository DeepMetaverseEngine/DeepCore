using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GUI;

namespace DeepCore.Unity.OnGUI
{
    public class GUIToggle : GUIObject
    {
        public bool Checked { get; set; }
        public GUIToggle() : base(new GUIStyle(UnityEngine.GUI.skin.toggle))
        {

        }
        protected override void Disposing()
        {
            CheckChanged = null;
        }
        protected override void OnVisit(GUIGraphics g)
        {
            var oldc = Checked;
            Checked = UnityEngine.GUI.Toggle(g.LocalBounds, Checked, Content, Style);
            if (oldc != Checked)
            {
                Input.ResetInputAxes();
                CheckChanged?.Invoke(this, Checked);
            }
        }
        public event Action<GUIToggle, bool> CheckChanged;
    }
}
