using DeepCore.GUI.Data;
using DeepCore.GUI.SceneGraph;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static DeepCore.Colors;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    [UEInstance(typeof(UECheckBoxMeta))]
    public class UECheckBox : UEDisplayNode<UECheckBoxMeta>
    {
        public UITextLayer TextLayer { get; private set; }
        public UITextLayer TextCheckLayer { get; private set; }
        public UIImageLayer ImageLayer { get; private set; }
        public UIImageLayer ImageCheckLayer { get; private set; }
        public UIImageLayer ImageUncheckLayer { get; private set; }

        public bool IsChecked { get => Meta.IsChecked; set => Meta.IsChecked = value; }
        public UECheckBox(UIFactory editor, UECheckBoxMeta e) : base(editor, e)
        {
            this.Rect.MouseDown += Rect_MouseDown;
        }
        public override string GetTextValue()
        {
            return Meta.Text;
        }
        private void Rect_MouseDown(InteractiveComponent sender, MouseArgs args)
        {
            Meta.IsChecked = !Meta.IsChecked;
        }
        protected override void DoDecodeFields()
        {
            base.DoDecodeFields();
            this.TextLayer = Editor.CreateTextLayer(Meta.Text, Meta.Font, Meta.TextStyle);
            this.AutoRelease(this.TextLayer);
            this.TextCheckLayer = Editor.CreateTextLayer(Meta.TextUnchecked, Meta.Font, Meta.CheckTextStyle);
            this.AutoRelease(this.TextCheckLayer);

            this.ImageLayer = Editor.CreateImageLayer(Meta.ImageBackAtlas, Meta.ImageStyle);
            this.AutoRelease(this.ImageLayer);
            this.ImageCheckLayer = Editor.CreateImageLayer(Meta.ImageCheckedAtlas, Meta.ImageStyle);
            this.AutoRelease(this.ImageCheckLayer);
            this.ImageUncheckLayer = Editor.CreateImageLayer(Meta.ImageUncheckedAtlas, Meta.ImageStyle);
            this.AutoRelease(this.ImageUncheckLayer);
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            base.OnDrawBegin(args);
            DrawCheckImage(args);
            DrawCheckText(args);
        }

        protected virtual void DrawCheckImage(GraphicsArgs args)
        {
            this.ImageLayer?.Render(args.Graphics, this.LocalBounds);
            if (Meta.IsChecked)
            {
                this.ImageCheckLayer?.Render(args.Graphics, this.LocalBounds);
            }
            else
            {
                this.ImageUncheckLayer?.Render(args.Graphics, this.LocalBounds);
            }
        }
        protected virtual void DrawCheckText(GraphicsArgs args)
        {
            this.TextLayer.Text = Meta.Text;
            this.TextLayer?.Render(args.Graphics, this.LocalBounds);
            this.TextCheckLayer.Text = Meta.IsChecked ? Meta.TextChecked : Meta.TextUnchecked;
            this.TextCheckLayer?.Render(args.Graphics, this.LocalBounds);
        }
    }
}
