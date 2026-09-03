using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.SceneGraph;
using System;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    [UEInstance(typeof(UERootMeta))]
    public class UERoot : UEContainerNode<UERootMeta>
    {
        public UERoot(UIFactory editor, UERootMeta e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Meta.EditorName;
        }
    }
}

