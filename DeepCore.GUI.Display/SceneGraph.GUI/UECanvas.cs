using DeepCore.GUI.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    [UEInstance(typeof(UECanvasMeta))]
    public class UECanvas : UEContainerNode<UECanvasMeta>
    {
        public UECanvas(UIFactory editor, UECanvasMeta e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Meta.EditorName;
        }
    }

}
