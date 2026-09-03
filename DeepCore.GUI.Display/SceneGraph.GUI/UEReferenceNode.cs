using DeepCore.GUI.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    [UEInstance(typeof(UEReferenceNodeMeta))]
    public class UEReferenceNode : UEDisplayNode<UEReferenceNodeMeta>
    {
        public UEReferenceNode(UIFactory editor, UEReferenceNodeMeta e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Meta.ReferenceGUID;
        }
    }
}
