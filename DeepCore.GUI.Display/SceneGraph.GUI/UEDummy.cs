using DeepCore.GUI.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    public class UEDummy : UEDisplayNode
    {
        public UEDummy(UIFactory editor, UEComponentMeta e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Meta.EditorName;
        }
    }
}
