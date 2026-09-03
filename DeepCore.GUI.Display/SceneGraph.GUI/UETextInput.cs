using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.Input;
using DeepCore.GUI.SceneGraph;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    public abstract class UETextInputBase : UEDisplayNode<UETextInputBaseMeta>
    {
        public StringBuilder Text { get; }
        public InputComponent Input { get; }
        public UITextLayer TextLayer { get; private set; }
        public UITextLayer PlaceHolderLayer { get; private set; }
        public Vector2 ScrollPosition { get; set; }
        public UETextInputBase(UIFactory editor, UETextInputBaseMeta e) : base(editor, e)
        {
            this.Text = new StringBuilder(e.Text);
            this.Input = this.Components.AddComponentAs<InputComponent>();
            Input.KeyPress += Input_KeyPress;
        }
        public override string GetTextValue()
        {
            return Text.ToString();
        }
        protected override void DoDecodeFields()
        {
            base.DoDecodeFields();
            this.TextLayer = Editor.CreateTextLayer(Meta.Text, Meta.Font, Meta.TextStyle);
            this.AutoRelease(this.TextLayer);
            this.PlaceHolderLayer = Editor.CreateTextLayer(Meta.PlaceHolder, Meta.Font, Meta.TextStyle);
            this.AutoRelease(this.PlaceHolderLayer);
        }
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            base.OnDrawBegin(args);
            DrawText(args);
            DrawCursor(args);
        }
        protected virtual void DrawText(GraphicsArgs args)
        {
            var text = Text.ToString();
            if (!string.IsNullOrEmpty(text))
            {
                this.TextLayer.Text = text;
                this.TextLayer.Render(args.Graphics, this.LocalBounds);              
            }
            else if (!string.IsNullOrEmpty(this.PlaceHolderLayer?.Text))
            {
                this.PlaceHolderLayer.Render(args.Graphics, this.LocalBounds);
            }
            else
            {
                this.TextLayer.Text = Meta.Text;
                this.TextLayer.Render(args.Graphics, this.LocalBounds);
            }
        }
        protected virtual void DrawCursor(GraphicsArgs args)
        {
            if (Root.FocusedInput == this.Input)
            {
                var c = Color.White;
                if (Meta.TextStyle != null)
                {
                    c = Meta.TextStyle.TextColor;
                }
                var bounds = this.LocalBounds;
                var d = (float)Math.Sin(Root.ElapsedTime.TotalMilliseconds / 100f);
                c = c.SetAlpha(0.5f + d * 0.5f);
                args.Graphics.SetColor(c);
                args.Graphics.FillRect(bounds.x, bounds.y + bounds.Height - 4, bounds.Width, 4);
            }
        }
        protected virtual void Input_KeyPress(InputComponent sender, KeyboardArgs args)
        {
            var oldText = this.Text.ToString();
            try
            {
                if (args.KeyChar == '\b')
                {
                    if (this.Text.Length > 0)
                    {
                        this.Text.Remove(this.Text.Length - 1, 1);
                    }
                }
                else
                {
                    this.Text.Append(args.KeyChar);
                }
            }
            finally
            {
                var newText = this.Text.ToString();
                if (oldText != newText)
                {
                    Invoke_TextChanged(newText, oldText);
                }
            }
        }

    }

    [UEInstance(typeof(UETextInputMeta))]
    public class UETextInput : UETextInputBase
    {
        public UETextInput(UIFactory editor, UETextInputMeta e) : base(editor, e)
        {
        }
    }

    [UEInstance(typeof(UETextInputMultilineMeta))]
    public class UETextInputMultiline : UETextInputBase
    {
        public UETextInputMultiline(UIFactory editor, UETextInputMultilineMeta e) : base(editor, e)
        {
        }
    }
}
