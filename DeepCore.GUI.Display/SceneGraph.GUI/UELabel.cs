using DeepCore.GUI.Data;
using DeepCore.GUI.SceneGraph;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{

    public abstract class UETextComponent<T> : UEDisplayNode<T> where T : UETextComponentMeta
    {
        public UITextLayer TextLayer { get; private set; }
        public UETextComponent(UIFactory editor, T e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Meta.Text;
        }
        protected override void DoBindData(string key, object value)
        {
            Meta.Text = $"{value}";
        }
        protected override void DoDecodeFields()
        {
            base.DoDecodeFields();
            this.TextLayer = Editor.CreateTextLayer(Meta.Text, Meta.Font, Meta.TextStyle);
            this.AutoRelease(this.TextLayer);
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            base.OnDrawBegin(args);
            DrawText(args);
        }
        protected virtual void DrawText(GraphicsArgs args)
        {
            this.TextLayer.Text = Meta.Text;
            this.TextLayer?.Render(args.Graphics, this.LocalBounds);
        }
    }

    [UEInstance(typeof(UELabelMeta))]
    public class UELabel : UETextComponent<UELabelMeta>
    {
        public UELabel(UIFactory editor, UELabelMeta e) : base(editor, e)
        {
        }

        protected override void DoBindData(string key, object value)
        {
            Meta.Text = $"{value}";
        }
    }
}
