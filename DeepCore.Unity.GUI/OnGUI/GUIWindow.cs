using DeepCore.Concurrent;
using DeepCore.Unity.OnGUI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepCore.Unity.OnGUI
{
    public class GUIWindow : GUIContainer
    {
        private static AtomicInteger idgen = new AtomicInteger(1);

        public int FormID { get; } = idgen.GetAndIncrement();
        public GUIWindow():base(UnityEngine.GUI.skin.window)
        {
            this.LocalPadding = new Padding(4, 24, 4, 4);
        }
        protected override void OnVisit(GUIGraphics g)
        {
            var parentSize = g.ParentSize;
            var bounds = GUI.Window(FormID, g.LocalBounds, id =>
            {
                using (var dg = new GUIGraphics())
                {
                    dg.ParentSize = parentSize;
                    dg.LocalBounds = new Rect(Vector2.zero, this.Size);
                    base.OnVisit(dg);
                }
                if (this.Dock == DockStyle.None && this.Anchor == AnchorStyles.None)
                {

                }
                GUI.DragWindow(new Rect(0, 0, parentSize.x, parentSize.y));
            }, Content, Style);
            if (this.Dock == DockStyle.None && this.Anchor == AnchorStyles.None)
            {
                this.Bounds = bounds;
            }
        }
    }
}
