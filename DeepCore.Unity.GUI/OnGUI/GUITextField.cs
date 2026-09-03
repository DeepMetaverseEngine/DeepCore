using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GUI;

namespace DeepCore.Unity.OnGUI
{
    public class GUITextField : GUIObject
    {
        public bool IsPassword { get; set; } = false;
        public GUITextField() : base(new GUIStyle(UnityEngine.GUI.skin.textField))
        {
        }
        protected override void Disposing()
        {
            TextChanged = null;
        }
        protected override void OnVisit(GUIGraphics g)
        {
            var oldText = Text;
            if (IsPassword)
            {
                this.Text = UnityEngine.GUI.PasswordField(g.LocalBounds, this.Text, '*', Style);
            }
            else
            {
                this.Text = UnityEngine.GUI.TextField(g.LocalBounds, this.Text, Style);
            }
            if (oldText != this.Text)
            {
                TextChanged?.Invoke(this, oldText, this.Text);
            }
        }
        public event Action<GUITextField, string, string> TextChanged;
    }
}
