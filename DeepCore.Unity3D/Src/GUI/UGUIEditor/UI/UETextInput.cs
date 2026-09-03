using DeepCore.GUI.Data;
using DeepCore.Unity3D.UGUI;
using UnityEngine;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUIEditor.UI
{
    public partial class UETextInput : UIComponent
    {
        protected readonly DisplayText mTextSprite;
        protected readonly DisplayText mPlaceHolder;
        protected readonly IInputField mInputField;

        protected float mBorderSize = 1;
        protected bool mLayoutDirty = true;

        protected readonly bool mUseBitmapFont;

        public UETextInput(string name, bool use_bitmap) : base(name)
        {
            this.mUseBitmapFont = use_bitmap;
            this.Enable = true;
            this.EnableChildren = false;
            if (use_bitmap)
            {
                InitWithBitmap(out mTextSprite, out mPlaceHolder, out mInputField);
            }
            else
            {
                InitWithText(out mTextSprite, out mPlaceHolder, out mInputField);
            }
            this.mInputField.event_ValueChanged += (onValueChanged);
            this.mInputField.event_EndEdit += (onEndEdit);
            this.IsInteractive = true;
        }
        public UETextInput() : this("", UIEditor.GlobalUseBitmapText)
        {
        }

        protected virtual void InitWithBitmap(out DisplayText text, out DisplayText placeholder, out IInputField inputfield)
        {
            var ph = new BitmapTextSprite("place_holder");
            ph.FontColor = Color.gray;
            this.AddChild(ph);

            var tx = new BitmapTextSprite("text");
            tx.FontColor = Color.white;
            tx.AutoScrollToCaret = true;
            this.AddChild(tx);

            var input = mGameObject.AddComponent<TextLayerInputField>();
            input.TextComponent = tx.Graphics;
            input.Placeholder = ph.Graphics;
            input.inputType = UnityEngine.UI.InputField.InputType.Standard;

            text = tx;
            placeholder = ph;
            inputfield = input;
        }
        protected virtual void InitWithText(out DisplayText text, out DisplayText placeholder, out IInputField inputfield)
        {
            var ph = new TextSprite("place_holder");
            ph.FontColor = Color.gray;
            ph.Graphics.resizeTextForBestFit = false;
            this.AddChild(ph);

            var tx = new TextSprite("text");
            tx.Graphics.supportRichText = false;
            tx.Graphics.resizeTextForBestFit = false;
            tx.Graphics.horizontalOverflow = HorizontalWrapMode.Wrap;
            tx.Graphics.verticalOverflow = VerticalWrapMode.Overflow;
            tx.FontColor = Color.white;
            this.AddChild(tx);

            var input = mGameObject.AddComponent<InteractiveInputField>();
            input.textComponent = tx.Graphics;
            input.placeholder = ph.Graphics;
            input.inputType = UnityEngine.UI.InputField.InputType.Standard;

            text = tx;
            placeholder = ph;
            inputfield = input;
        }
        protected override IInteractiveComponent GenInteractive()
        {
            return mInputField;
        }

        public DisplayText TextSprite { get { return mTextSprite; } }
        public DisplayText PlaceHolder { get { return mPlaceHolder; } }
        public IInputField Input { get { return mInputField; } }

        public float BorderSize
        {
            get { return mBorderSize; }
            set
            {
                if (mBorderSize != value)
                {
                    this.mBorderSize = value;
                    this.mLayoutDirty = true;
                }
            }
        }
        public string Text
        {
            get { return mTextSprite.Text; }
            set
            {
                if (IsDispose) return;
                mTextSprite.Text = value;
            }
        }
        public string PlaceHolderText
        {
            get { return mPlaceHolder.Text; }
            set
            {
                if (IsDispose) return;
                mPlaceHolder.Text = value;
            }
        }
        public int FontSize
        {
            get { return mTextSprite.FontSize; }
            set
            {
                mTextSprite.FontSize = value;
                mPlaceHolder.FontSize = value;
            }
        }
        public GUI.Data.FontStyle Style
        {
            get { return TextSprite.Style; }
            set
            {
                TextSprite.Style = value;
                PlaceHolder.Style = value;
            }
        }
        public UnityEngine.Color FontColor
        {
            get { return mTextSprite.FontColor; }
            set
            {
                mTextSprite.FontColor = value;
                value.a = value.a / 2;
                mPlaceHolder.FontColor = value;
            }
        }
        public void SetFont(Font font)
        {
            TextSprite.SetFont(font);
            PlaceHolder.SetFont(font);
        }

        public void SetInputTextAnchor(GUI.Data.TextAnchor value)
        {
            TextSprite.Anchor = value;
            PlaceHolder.Anchor = value;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (mLayoutDirty)
            {
                this.mLayoutDirty = false;
                Vector2 csize = this.Size2D;
                this.mPlaceHolder.Bounds2D = this.mTextSprite.Bounds2D = new Rect(
                    mBorderSize,
                    mBorderSize,
                    csize.x - mBorderSize * 2,
                    csize.y - mBorderSize * 2);
            }

            if (this.Input.isFocused)
            {
                    this.mPlaceHolder.Visible = false;
            }
            else
            {              
                    this.mPlaceHolder.Visible = true;
            }

        }



        protected override void DecodeBegin(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeBegin(editor, e);
        }

        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);
            this.Decode_Text(editor, e as UETextInputBaseMeta);
            this.BorderSize = (this.Layout != null) ? this.Layout.BorderSize : 1;
            this.Enable = true;
            this.EnableChildren = false;

            if(UIEditorMeta.UIEditorRunTime)
            {
                this.Enable = e.Enable;
                this.EnableChildren = e.EnableChilds;
                this.IsInteractive = true;
            }
        }

        private void Decode_Text(UIEditor.Decoder editor, UETextInputBaseMeta e)
        {
            if (e.textFontSize > 0)
            {
                this.FontSize = e.textFontSize;
            }
            if (!string.IsNullOrEmpty(e.textFontName))
            {
                this.SetFont(editor.editor.CreateFont(e.textFontName));
                this.Style = e.textFontStyle;
            }
            this.FontColor = UIUtils.UInt32_ARGB_To_Color(e.textColor);
            this.Input.inputType = e.isPassword ? InputField.InputType.Password : InputField.InputType.Standard;
            this.PlaceHolderText = e.Text;
        }


        //-----------------------------------------------------------------------------------------------------
        #region _Event_

        protected override void OnDisposeEvents()
        {
            this.event_ValueChanged = null;
            this.event_endEdit = null;

            if (mInputField != null)
            {
                event_PointerClick = null;
            }

            base.OnDisposeEvents();
        }

        private void onValueChanged(string value)
        {
            if (event_ValueChanged != null)
                event_ValueChanged.Invoke(this, value);
        }
        private void onEndEdit(string value)
        {
            if (event_endEdit != null)
                event_endEdit.Invoke(this, value);
        }


        public delegate void InputValueChangedHandler(DisplayNode sender, string text);

        public InputValueChangedHandler event_ValueChanged;
        public InputValueChangedHandler event_endEdit;

        public event InputValueChangedHandler ValueChanged { add { event_ValueChanged += value; } remove { event_ValueChanged -= value; } }
        public event InputValueChangedHandler EndEdit { add { event_endEdit += value; } remove { event_endEdit -= value; } }


        #endregion
        //-----------------------------------------------------------------------------------------------------

    }

    //----------------------------------------------------------------------
    public class UETextInputMultiline : UETextInput
    {
        public UETextInputMultiline(string name, bool use_bitmap)
            : base(name, use_bitmap)
        {
        }
        public UETextInputMultiline() : this("", UIEditor.GlobalUseBitmapText)
        {
        }

        protected override void InitWithBitmap(out DisplayText text, out DisplayText placeholder, out IInputField inputfield)
        {
            var ph = new BitmapTextSprite("place_holder");
            ph.FontColor = Color.gray;
            this.AddChild(ph);

            var tx = new RichTextPan(true, "rich_text");
            tx.FontColor = Color.white;
            tx.AutoScrollToCaret = true;
            this.AddChild(tx);

            var input = mGameObject.AddComponent<TextLayerInputField>();
            input.TextComponent = tx;
            input.Placeholder = ph.Graphics;
            input.inputType = UnityEngine.UI.InputField.InputType.Standard;
            input.lineType = InputField.LineType.MultiLineNewline;
            input.characterLimit = 500;

            text = tx;
            placeholder = ph;
            inputfield = input;
        }
        protected override void InitWithText(out DisplayText text, out DisplayText placeholder, out IInputField inputfield)
        {
            base.InitWithText(out text, out placeholder, out inputfield);
            var tx = text as TextSprite;
            var tf = inputfield as InteractiveInputField;
            tf.lineType = InputField.LineType.MultiLineNewline;
        }
    }
}
