using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GUI;

namespace DeepCore.Unity.OnGUI
{
    public class GUITextArea : GUIObject
    {
        public GUITextArea() : base(new GUIStyle(UnityEngine.GUI.skin.textArea))
        {

        }
        protected override void Disposing()
        {

        }
        protected override void OnVisit(GUIGraphics g)
        {
            var oldText = Text;
            this.Text = UnityEngine.GUI.TextArea(g.LocalBounds, this.Text, Style);
            if (oldText != this.Text)
            {
                TextChanged?.Invoke(this, oldText, this.Text);
            }
        }
        public event Action<GUITextArea, string, string> TextChanged;
    }

}
