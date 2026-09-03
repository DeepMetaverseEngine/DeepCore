using DeepCore.Geometry;
using DeepCore.GUI.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    [UEInstance(typeof(UEScrollPanMeta))]
    public class UEScrollPan : UEContainerNode<UEScrollPanMeta>
    {
        public Vector2 ScrollPosition { get; set; }
        public UEScrollPan(UIFactory editor, UEScrollPanMeta e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Meta.EditorName;
        }
    }
}
