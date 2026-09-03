using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GUI;

namespace DeepCore.Unity.OnGUI
{
    public class GUIPanel : GUIContainer
    {
        private Vector2 scrollPosition;
        public bool AlwaysShowHorizontal;
        public bool AlwaysShowVertical;
        public GUIStyle HorizontalScrollbarStyle { get; set; }
        public GUIStyle VerticalScrollbarStyle { get; set; }
        public GUIPanel() : base(new GUIStyle(UnityEngine.GUI.skin.scrollView))
        {
            this.HorizontalScrollbarStyle = new GUIStyle(UnityEngine.GUI.skin.horizontalScrollbar);
            this.VerticalScrollbarStyle = new GUIStyle(UnityEngine.GUI.skin.verticalScrollbar);
            this.LocalPadding = new Padding(4, 4, 24, 24);
        }
        protected override void OnVisit(GUIGraphics g)
        {
            //GUI.color = Color.red;
            var childBounds = ChildTotalBounds;
            scrollPosition = MyGUI.BeginScrollView(
                g.LocalBounds, scrollPosition, childBounds,
                AlwaysShowHorizontal,
                AlwaysShowVertical,
                HorizontalScrollbarStyle,
                VerticalScrollbarStyle,
                Style);
            try
            {
                base.OnVisit(g);
            }
            finally
            {
                UnityEngine.GUI.EndScrollView();
            }
        }
    }
}
