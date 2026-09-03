using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.SceneGraph;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace DeepCore.GUI.SceneGraph
{
    public class DisplayInteracviteNode : DisplayNode
    {
        public RectInteracviteComponent Rect { get; }
        public RectangleF LocalBounds { get => Rect.Bounds; set => Rect.Bounds = value; }
        public DisplayInteracviteNode()
        {
            this.Rect = this.Components.AddComponent<RectInteracviteComponent>();
        }
    }
}
