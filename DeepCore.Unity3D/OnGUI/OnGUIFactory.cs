using DeepCore.Concurrent;
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.SceneGraph;
using UnityEngine;

namespace DeepCore.Unity3D.Impl.OnGUI
{
    public partial class OnGUIFactory : UIFactory
    {
        public OnGUIFactory(string rootDir) : base(rootDir)
        {
        }
        //-------------------------------------------------------------------------------------------------------------------------
        public GUISkin Skin => LazyGetGUISkin();
        private GUISkin _skin;
        protected GUISkin LazyGetGUISkin()
        {
            return UnityEngine.GUI.skin;
        }
        //-------------------------------------------------------------------------------------------------------------------------

        protected override UEComponentNode DoCreateUI(UEComponentMeta meta)
        {
            if (meta == null) return null;
            if (meta is UELabelMeta metaLabel) return new OnGUILabel(this, metaLabel);
            if (meta is UEGaugeMeta metaGauge) return new OnGUIGauge(this, metaGauge);
            if (meta is UEImageBoxMeta metaImageBox) return new OnGUIImageBox(this, metaImageBox);
            if (meta is UETextButtonMeta metaButton) return new OnGUITextButton(this, metaButton);
            if (meta is UEToggleButtonMeta metaToggle) return new OnGUIToggleButton(this, metaToggle);
            if (meta is UECheckBoxMeta metaCheckBox) return new OnGUICheckBox(this, metaCheckBox);
            if (meta is UERootMeta metaRoot) return new OnGUIForm(this, metaRoot);
            if (meta is UERichTextBoxMeta metaRichText) return new OnGUIRichTextBox(this, metaRichText);
            if (meta is UETextBoxMeta metaTextBox) return new OnGUITextBox(this, metaTextBox);
            if (meta is UETextInputMeta metaTextInput) return new OnGUITextInput(this, metaTextInput);
            if (meta is UETextInputMultilineMeta metaInputMulti) return new OnGUITextInputMultiline(this, metaInputMulti);
            if (meta is UECanvasMeta metaCanvas) return new OnGUICanvasPan(this, metaCanvas);
            if (meta is UEScrollPanMeta metaScrollPan) return new OnGUIScrollPan(this, metaScrollPan);

            if (meta is UEReferenceNodeMeta metaReference) return new UEReferenceNode(this, metaReference);

            return base.DoCreateUI(meta);
        }
        //-------------------------------------------------------------------------------------------------------------------------
        public static TextAnchor CONVERT_TEXT_ANCHOR(AlignmentStyle meta)
        {
            switch (meta)
            {
                case AlignmentStyle.TopLeft: return TextAnchor.UpperLeft;
                case AlignmentStyle.TopCenter: return TextAnchor.UpperCenter;
                case AlignmentStyle.TopRight: return TextAnchor.UpperRight;
                case AlignmentStyle.MiddleLeft: return TextAnchor.MiddleLeft;
                case AlignmentStyle.MiddleCenter: return TextAnchor.MiddleCenter;
                case AlignmentStyle.MiddleRight: return TextAnchor.MiddleRight;
                case AlignmentStyle.BottomLeft: return TextAnchor.LowerLeft;
                case AlignmentStyle.BottomCenter: return TextAnchor.LowerCenter;
                case AlignmentStyle.BottomRight: return TextAnchor.LowerRight;
            }
            return TextAnchor.MiddleCenter;
        }
        public static FontStyle CONVERT_FONT_STYLE(TextFontStyle meta)
        {
            switch (meta)
            {
                case TextFontStyle.Plain: return FontStyle.Normal;
                case TextFontStyle.Bold: return FontStyle.Bold;
                case TextFontStyle.Italic: return FontStyle.Italic;
                case TextFontStyle.BoldAndItalic: return FontStyle.BoldAndItalic;
            }
            return FontStyle.Normal;
        }
        public static GUIStyle CONVERT_STYLE(UITextStyleMeta meta, UIFontMeta font, GUIStyle style)
        {
            //style = new GUIStyle(style);
            if (meta != null)
            {
                style.alignment = CONVERT_TEXT_ANCHOR(meta.Align);
                style.normal = new GUIStyleState()
                {
                    textColor = meta.TextColor.ToUnityColor(),
                };
                //                 style.active = new GUIStyleState()
                //                 {
                //                     textColor = meta.TextColor.ToUnityColor(),
                //                 };
                //                 style.hover = new GUIStyleState()
                //                 {
                //                     textColor = meta.TextColor.ToUnityColor(),
                //                 };
                //                 style.focused = new GUIStyleState()
                //                 {
                //                     textColor = meta.TextColor.ToUnityColor(),
                //                 };
            }
            if (font != null)
            {
                style.fontSize = (int)font.Size;
                style.fontStyle = CONVERT_FONT_STYLE(font.Style);
            }
            return style;
        }
        public static void BEGIN_STYLE(GUI.Display.Graphics gfx, UITextStyleMeta meta, UIFontMeta font, GUIStyle style)
        {
            if (font != null)
            {
                style.fontSize = (int)font.Size;
            }
            else
            {
                style.fontSize = (int)gfx.CurrentFontSize;
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------
        class OnGUIForm : UERoot
        {
            private static AtomicInteger idgen = new AtomicInteger(1);
            public int FormID { get; } = idgen.GetAndIncrement();
            public OnGUIForm(UIFactory editor, UERootMeta e) : base(editor, e)
            {
            }
            protected override void ApplyTransform(GraphicsArgs args)
            {
                if (Meta.Dock == DockStyle.None && Meta.Anchor == AlignmentStyle.None)
                {
                }
                else
                {
                    base.ApplyTransform(args);
                }
            }
            protected override void VisitChilds(GraphicsArgs args)
            {
                if (Meta.Dock == DockStyle.None && Meta.Anchor == AlignmentStyle.None)
                {
                }
                else
                {
                    base.VisitChilds(args);
                }
            }
            protected override void OnDrawBegin(GraphicsArgs args)
            {
                if (Meta.Layout != null)
                {
                    UnityEngine.GUI.backgroundColor = Meta.Layout.BackColor.ToUnityColor();
                }
                if (Meta.Dock == DockStyle.None && Meta.Anchor == AlignmentStyle.None)
                {
                    var pos = this.Position.ToUnity();
                    var size = this.Rect.Bounds.Size.ToUnity();
                    var bounds = UnityEngine.GUI.Window(FormID, new UnityEngine.Rect(pos, size), id =>
                    {
                        Layout?.Render(args.Graphics, new Geometry.RectangleF(0, 0, size.x, size.y));
                        base.VisitChilds(args);
                        UnityEngine.GUI.DragWindow(new Rect(0, 0, size.x, size.y));
                    }, Meta.Text);
                    this.Position = new Geometry.Vector2(bounds.x, bounds.y);
                }
                else
                {
                    base.OnDrawBegin(args);
                }
            }
            protected override void OnDrawAfter(GraphicsArgs args)
            {

            }
        }
        class OnGUILabel : UELabel
        {
            GUIContent content;
            GUIStyle style;
            public OnGUILabel(OnGUIFactory editor, UELabelMeta e) : base(editor, e)
            {
                this.content = new GUIContent()
                {
                    text = e.Text,
                    tooltip = e.ToolTipText,
                };
                this.style = CONVERT_STYLE(Meta.TextStyle, Meta.Font, new GUIStyle(editor.Skin.label));
            }
            protected override void DrawText(GraphicsArgs args)
            {
                var bounds = this.LocalBounds;
                if (Meta.TextStyle != null)
                {
                    UnityEngine.GUI.contentColor = Meta.TextStyle.TextColor.ToUnityColor();
                }
                content.text = Meta.Text;
                BEGIN_STYLE(args.Graphics, Meta.TextStyle, Meta.Font, this.style);
                UnityEngine.GUI.Label(bounds.ToUnity(), content, style);
            }
        }
        class OnGUIGauge : UEGauge
        {
            public OnGUIGauge(OnGUIFactory editor, UEGaugeMeta e) : base(editor, e)
            {
            }
        }
        class OnGUIImageBox : UEImageBox
        {
            public OnGUIImageBox(OnGUIFactory editor, UEImageBoxMeta e) : base(editor, e)
            {
            }
        }
        class OnGUITextButton : UETextButton
        {
            GUIContent content;
            GUIStyle style;
            public OnGUITextButton(OnGUIFactory editor, UETextButtonMeta e) : base(editor, e)
            {
                this.content = new GUIContent()
                {
                    text = e.Text,
                    tooltip = e.ToolTipText,
                };
                this.style = CONVERT_STYLE(Meta.TextStyle, Meta.Font, new GUIStyle(editor.Skin.button));
            }
            protected override void DrawText(GraphicsArgs args)
            {
                args.Graphics.PushColor();
                try
                {
                    var bounds = this.LocalBounds;
                    if (Meta.TextStyle != null)
                    {
                        UnityEngine.GUI.contentColor = Meta.TextStyle.TextColor.ToUnityColor();
                    }
                    BEGIN_STYLE(args.Graphics, Meta.TextStyle, Meta.Font, this.style);
                    if (UnityEngine.GUI.Button(bounds.ToUnity(), content, style))
                    {
                        InvokeClick(OnGUICanvas.NewMouseArgs());
                    }
                }
                finally
                {
                    args.Graphics.PopColor();
                }
            }
        }
        class OnGUIToggleButton : UEToggleButton
        {
            GUIContent content;
            GUIStyle style;
            public OnGUIToggleButton(OnGUIFactory editor, UEToggleButtonMeta e) : base(editor, e)
            {
                this.content = new GUIContent()
                {
                    text = e.Text,
                    tooltip = e.ToolTipText,
                };
                this.style = CONVERT_STYLE(Meta.TextStyle, Meta.Font, new GUIStyle(editor.Skin.toggle));
            }
            protected override void DrawText(GraphicsArgs args)
            {
                args.Graphics.PushColor();
                try
                {
                    var bounds = this.LocalBounds;
                    if (Meta.TextStyle != null)
                    {
                        UnityEngine.GUI.contentColor = Meta.TextStyle.TextColor.ToUnityColor();
                    }
                    var old = IsChecked;
                    BEGIN_STYLE(args.Graphics, Meta.TextStyle, Meta.Font, this.style);
                    IsChecked = UnityEngine.GUI.Toggle(bounds.ToUnity(), IsChecked, content, style);
                    if (old != IsChecked)
                    {
                        Invoke_CheckedChanged(IsChecked);
                    }
                }
                finally
                {
                    args.Graphics.PopColor();
                }
            }
        }
        class OnGUICheckBox : UECheckBox
        {
            GUIContent content;
            GUIStyle style;
            public OnGUICheckBox(OnGUIFactory editor, UECheckBoxMeta e) : base(editor, e)
            {
                this.content = new GUIContent()
                {
                    text = e.Text,
                    tooltip = e.ToolTipText,
                };
                this.style = CONVERT_STYLE(Meta.TextStyle, Meta.Font, new GUIStyle(editor.Skin.toggle));
            }
            protected override void DrawCheckText(GraphicsArgs args)
            {
                var bounds = this.LocalBounds;
                if (Meta.TextStyle != null)
                {
                    UnityEngine.GUI.contentColor = Meta.TextStyle.TextColor.ToUnityColor();
                }
                var old = IsChecked;
                BEGIN_STYLE(args.Graphics, Meta.TextStyle, Meta.Font, this.style);
                IsChecked = UnityEngine.GUI.Toggle(bounds.ToUnity(), IsChecked, content);
                if (old != IsChecked)
                {
                    Invoke_CheckedChanged(IsChecked);
                }
            }
        }
        class OnGUITextInput : UETextInput
        {
            GUIStyle style;
            public OnGUITextInput(OnGUIFactory editor, UETextInputMeta e) : base(editor, e)
            {
                this.style = CONVERT_STYLE(Meta.TextStyle, Meta.Font, new GUIStyle(editor.Skin.textField));
            }
            protected override void DrawText(GraphicsArgs args)
            {
                var bounds = this.LocalBounds;
                var oldText = this.Text.ToString();
                if (string.IsNullOrEmpty(oldText))
                {
                    this.PlaceHolderLayer?.Render(args.Graphics, this.LocalBounds);
                }
                if (Meta.TextStyle != null)
                {
                    UnityEngine.GUI.contentColor = Meta.TextStyle.TextColor.ToUnityColor();
                }
                BEGIN_STYLE(args.Graphics, Meta.TextStyle, Meta.Font, this.style);
                var newText = UnityEngine.GUI.TextField(bounds.ToUnity(), oldText, style);
                if (!newText.Equals(oldText))
                {
                    this.Text.Clear();
                    this.Text.Append(newText);
                    Invoke_TextChanged(newText, oldText);
                }
            }
        }
        class OnGUITextInputMultiline : UETextInputMultiline
        {
            GUIStyle style;
            public OnGUITextInputMultiline(OnGUIFactory editor, UETextInputMultilineMeta e) : base(editor, e)
            {
                this.style = CONVERT_STYLE(Meta.TextStyle, Meta.Font, new GUIStyle(editor.Skin.textArea));
                this.style.wordWrap = true;
            }
            protected override void DrawText(GraphicsArgs args)
            {
                var bounds = this.LocalBounds;
                var oldText = this.Text.ToString();
                if (string.IsNullOrEmpty(oldText))
                {
                    this.PlaceHolderLayer?.Render(args.Graphics, this.LocalBounds);
                }
                if (Meta.TextStyle != null)
                {
                    UnityEngine.GUI.contentColor = Meta.TextStyle.TextColor.ToUnityColor();
                }
                BEGIN_STYLE(args.Graphics, Meta.TextStyle, Meta.Font, this.style);
                var newText = UnityEngine.GUI.TextArea(bounds.ToUnity(), oldText, style);
                if (!newText.Equals(oldText))
                {
                    this.Text.Clear();
                    this.Text.Append(newText);
                    Invoke_TextChanged(newText, oldText);
                }
            }
        }

        class OnGUITextBox : UETextBox
        {
            GUIContent content;
            GUIStyle style;
            public OnGUITextBox(OnGUIFactory editor, UETextBoxMeta e) : base(editor, e)
            {
                this.content = new GUIContent()
                {
                    text = e.Text,
                    tooltip = e.ToolTipText,
                };
                this.style = CONVERT_STYLE(Meta.TextStyle, Meta.Font, new GUIStyle(editor.Skin.box));
                this.style.wordWrap = true;
            }
            protected override void DrawText(GraphicsArgs args)
            {
                var bounds = this.LocalBounds;
                //                 var scroll = this.ScrollPosition;
                //                 var rect = Meta.Padding.Cut(bounds);
                //                 var scrollP = UnityEngine.GUI.BeginScrollView(bounds.ToUnity(), scroll.ToUnity(), new UnityEngine.Rect(rect.X, rect.Y, rect.Width, rect.Height));
                //                 this.ScrollPosition = new Geometry.Vector2(scrollP.x, scrollP.y);
                if (Meta.TextStyle != null)
                {
                    UnityEngine.GUI.contentColor = Meta.TextStyle.TextColor.ToUnityColor();
                }
                BEGIN_STYLE(args.Graphics, Meta.TextStyle, Meta.Font, this.style);
                UnityEngine.GUI.Box(bounds.ToUnity(), content, style);
            }
        }
        class OnGUIRichTextBox : UERichTextBox
        {
            GUIStyle style;
            public OnGUIRichTextBox(OnGUIFactory editor, UERichTextBoxMeta e) : base(editor, e)
            {
                this.style = CONVERT_STYLE(Meta.TextStyle, Meta.Font, new GUIStyle(editor.Skin.box));
                this.style.wordWrap = true;
            }
            protected override void DrawText(GraphicsArgs args)
            {
                var bounds = this.LocalBounds;
                //                 var scroll = this.ScrollPosition;
                //                 var rect = Meta.Padding.Cut(bounds);
                //                 var scrollP = UnityEngine.GUI.BeginScrollView(bounds.ToUnity(), scroll.ToUnity(), new UnityEngine.Rect(rect.X, rect.Y, rect.Width, rect.Height));
                //                 this.ScrollPosition = new Geometry.Vector2(scrollP.x, scrollP.y);
                if (Meta.TextStyle != null)
                {
                    UnityEngine.GUI.contentColor = Meta.TextStyle.TextColor.ToUnityColor();
                }
                BEGIN_STYLE(args.Graphics, Meta.TextStyle, Meta.Font, this.style);
                UnityEngine.GUI.Box(bounds.ToUnity(), this.Text.ToString(), style);
            }
        }
        class OnGUICanvasPan : UECanvas
        {
            public OnGUICanvasPan(OnGUIFactory editor, UECanvasMeta e) : base(editor, e)
            {
            }
            //             protected override void OnDrawBegin(GraphicsArgs args)
            //             {
            //                 if (Meta.Layout != null)
            //                 {
            //                     UnityEngine.GUI.backgroundColor = Meta.Layout.BackColor.ToUnityColor();
            //                 }
            //                 var bounds = this.LocalBounds;
            //                 UnityEngine.GUI.BeginGroup(bounds.ToUnity());
            //             }
            //             protected override void OnDrawAfter(GraphicsArgs args)
            //             {
            //                 UnityEngine.GUI.EndGroup();
            //             }
        }
        class OnGUIScrollPan : UEScrollPan
        {
            public OnGUIScrollPan(OnGUIFactory editor, UEScrollPanMeta e) : base(editor, e)
            {
            }
            protected override void OnDrawBegin(GraphicsArgs args)
            {
                base.OnDrawBegin(args);
                var bounds = this.LocalBounds;
                var scroll = this.ScrollPosition;
                var rect = Meta.Padding.Cut(bounds);
                var scrollP = UnityEngine.GUI.BeginScrollView(bounds.ToUnity(), scroll.ToUnity(),
                    new UnityEngine.Rect(rect.X, rect.Y, rect.Width, rect.Height));
                this.ScrollPosition = new Geometry.Vector2(scrollP.x, scrollP.y);
            }
            protected override void OnDrawAfter(GraphicsArgs args)
            {
                UnityEngine.GUI.EndScrollView(true);
            }
        }
    }


}
