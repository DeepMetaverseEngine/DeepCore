
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;

namespace DeepCore.GUI.Display.UI
{
    public class UICanvas : UIComponent
    {
        private TRectangle2D rect;
        public UICanvas()
        {
        }
        public override void Visit(Display.Graphics g)
        {
            if (ScrollRect != null)
            {
                g.PushClip();
                //渲染优化，使用结构体避免不必要的内存开销.
                rect = this.LocalToGlobal_S(ScrollRect);
                g.SetClip(rect.x, rect.y, rect.width, rect.height);
                base.Visit(g);
                g.PopClip();
            }
            else
            {
                base.Visit(g);
            }
        }
        protected override void DecodeFields(UIEditor editor, UIComponentMeta e)
        {
            base.DecodeFields(editor, e);

            this.Enable = e.Enable;
            this.EnableChildren = e.EnableChilds;
        }
    }
}
