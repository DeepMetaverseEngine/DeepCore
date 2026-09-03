using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.SceneGraph;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    [UEInstance(typeof(UEImageBoxMeta))]
    public class UEImageBox : UEDisplayNode<UEImageBoxMeta>
    {
        public UIImageLayer ImageLayer { get; private set; }
        public UEImageBox(UIFactory editor, UEImageBoxMeta e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Meta.EditorName;
        }
        protected override void DoBindData(string key, object value)
        {
            if (value is Image ftext)
            {
                ImageLayer?.SetImage(new UIResourceImage(ftext));
            }
        }
        protected override void DoDecodeFields()
        {
            base.DoDecodeFields();
            this.ImageLayer = Editor.CreateImageLayer(Meta.ImageAtlas, Meta.ImageStyle);
            this.AutoRelease(this.ImageLayer);
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            base.OnDrawBegin(args);
            DrawImage(args);
        }

        protected virtual void DrawImage(GraphicsArgs args)
        {
            this.ImageLayer?.Render(args.Graphics, this.LocalBounds);
        }
    }
}
