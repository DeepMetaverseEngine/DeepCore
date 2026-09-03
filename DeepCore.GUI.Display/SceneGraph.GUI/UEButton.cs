using DeepCore.GUI.Data;
using DeepCore.GUI.SceneGraph;
using System.Threading.Tasks;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    public abstract class UEButton : UEDisplayNode<UEButtonMeta>
    {
        public UITextLayer TextLayer { get; private set; }
        public UILayout LayoutDown { get; protected set; }
        public virtual bool IsDown { get => Rect.IsMouseDown; }
        public UEButton(UIFactory editor, UEButtonMeta e) : base(editor, e)
        {
        }
        public override string GetTextValue()
        {
            return Meta.Text;
        }
        protected override void DoDecodeFields()
        {
            base.DoDecodeFields();
            this.TextLayer = Editor.CreateTextLayer(Meta.Text, Meta.Font, Meta.TextStyle);
            this.AutoRelease(this.TextLayer);
            this.LayoutDown = Editor.CreateLayout(Meta.DownLayout);
            this.AutoRelease(this.LayoutDown);
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            base.OnDrawBegin(args);
            DrawText(args);
        }
        protected void InvokeClick(MouseArgs args)
        {
            Rect.Canvas_MouseClick(args);
        }
        protected override void DrawLayout(GraphicsArgs args)
        {
            var bounds = Rect.Bounds;
            if (Rect.Enable)
            {
                if (IsDown)
                {
                    this.LayoutDown?.Render(args.Graphics, bounds);
                }
                else
                {
                    this.Layout?.Render(args.Graphics, bounds);
                }
            }
            else
            {
                this.LayoutDisable?.Render(args.Graphics, bounds);
            }
        }
        protected virtual void DrawText(GraphicsArgs args)
        {
            var bounds = Rect.Bounds;
            if (Rect.Enable)
            {
                if (IsDown)
                {
                    if (!string.IsNullOrEmpty(Meta.DownText))
                    {
                        this.TextLayer.Text = Meta.DownText;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(Meta.Text))
                    {
                        this.TextLayer.Text = Meta.Text;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(Meta.Text))
                {
                    this.TextLayer.Text = Meta.Text;
                }
            }
            this.TextLayer?.Render(args.Graphics, bounds);
        }
    }

    [UEInstance(typeof(UETextButtonMeta))]
    public class UETextButton : UEButton
    {
        public UETextButton(UIFactory editor, UETextButtonMeta e) : base(editor, e)
        {
        }
    }
    [UEInstance(typeof(UEToggleButtonMeta))]
    public class UEToggleButton : UEButton
    {
        new public UEToggleButtonMeta Meta { get => base.Meta as UEToggleButtonMeta; }
        public bool IsChecked { get => Meta.IsChecked; set => Meta.IsChecked = value; }
        public override bool IsDown => this.IsChecked;
        public UEToggleButton(UIFactory editor, UEToggleButtonMeta e) : base(editor, e)
        {
            this.Rect.MouseDown += Rect_MouseDown;
        }
        private void Rect_MouseDown(InteractiveComponent sender, MouseArgs args)
        {
            Meta.IsChecked = !Meta.IsChecked;
        }
        //         protected override void DoDecodeFields()
        //         {
        //             base.DoDecodeFields();
        //         }
        public override string GetTextValue()
        {
            return Meta.Text;
        }
    }
}
