using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.SceneGraph;
using DeepMetaGame.Data.GUI.Meta;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepMetaGame.Display.GUI
{

    abstract public class UEInfo<ET> : UEDisplayNode<ET> where ET : UETextBoxBaseMeta
    {
        public UITextLayer TextLayer { get; private set; }
        public UEInfo(UIFactory editor, ET e) : base(editor, e)
        {
        }
        protected override void DoDecodeFields()
        {
            base.DoDecodeFields();
            this.TextLayer = Editor.CreateTextLayer("", Meta.Font, Meta.TextStyle);
            this.AutoRelease(this.TextLayer);
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            base.OnDrawBegin(args);
            DrawText(args);
        }
        protected virtual void DrawText(GraphicsArgs args)
        {
            this.TextLayer.Text = GetInfoText();
            this.TextLayer?.Render(args.Graphics, this.LocalBounds);
        }
        protected abstract string GetInfoText();
    }



    [UEInstance(typeof(UEMemoryAllocInfoMeta))]
    public class UEMemoryAllocInfo : UEInfo<UEMemoryAllocInfoMeta>
    {
        public UEMemoryAllocInfo(UIFactory editor, UEMemoryAllocInfoMeta e) : base(editor, e)
        {
        }
        protected override string GetInfoText()
        {
            return TypeAllocRecorder.GetMemoryStatus("  ", 8, 32);
        }
    }

}
