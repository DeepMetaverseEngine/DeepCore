
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;

namespace DeepCore.GUI.Display.UI
{
    public class UIRoot : UIComponent
    {

        protected override void DecodeFields(UIEditor editor, UIComponentMeta e)
        {
            base.DecodeFields(editor, e);

            this.Enable = e.Enable;
            this.EnableChildren = e.EnableChilds;
        }
    }
}

