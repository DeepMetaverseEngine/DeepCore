using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{
    public partial class TextLayerInputField : DisplayNodeInteractive, IEventSystemHandler, IUpdateSelectedHandler, ISubmitHandler, ICanvasElement, IInputField
    {
        //--------------------------------------------------------------------
        // property //
        private string m_Text = string.Empty;
        private char m_AsteriskChar = '*';
        private int m_CharacterLimit = 100;
        private InputField.ContentType m_ContentType = InputField.ContentType.Standard;
        private InputField.CharacterValidation m_CharacterValidation = InputField.CharacterValidation.None;
        private InputField.InputType m_InputType = InputField.InputType.Standard;
        private InputField.LineType m_LineType = InputField.LineType.SingleLine;
        private TouchScreenKeyboardType m_KeyboardType = TouchScreenKeyboardType.Default;
        private ITextComponent m_TextComponent;
        private Graphic m_Placeholder;
        private bool m_ShowCaret = true;
        private float m_CaretBlinkRate = 0.4f;
        public Action<string> event_EndEdit { get; set; }
        public Action<string> event_ValueChanged { get; set; }
        //--------------------------------------------------------------------
        // runtime //
        private Event m_ProcessingEvent = new Event();
        private TouchScreenKeyboard m_Keyboard = null;
        private string m_OriginalText = string.Empty;
        private bool m_WasCanceled = false;
        private bool m_ShouldActivateNextUpdate = false;
        private bool m_AllowInput = false;
        private CaretSprite m_CaretSprite;
        //--------------------------------------------------------------------
        public string Text
        {
            get
            {
                if (this.m_Keyboard != null && this.m_Keyboard.active && !this.InPlaceEditing() && EventSystem.current.currentSelectedGameObject == base.gameObject)
                {
                    return this.m_Keyboard.text;
                }
                return this.m_Text;
            }
            set
            {
                if (this.Text == value)
                {
                    return;
                }
                this.m_Text = value;
                if (!Application.isPlaying)
                {
                    this.SendOnValueChangedAndUpdateLabel();
                    return;
                }
                else
                {
                    if (this.m_Keyboard != null)
                    {
                        this.m_Keyboard.text = this.m_Text;
                    }
                    this.SendOnValueChangedAndUpdateLabel();
                }
            }
        }
        public bool isFocused
        {
            get { return this.m_AllowInput; }
        }
        public InputField.InputType inputType
        {
            get { return m_InputType; }
            set { m_InputType = value; }
        }
        public InputField.ContentType contentType
        {
            get { return m_ContentType; }
            set { m_ContentType = value; }
        }
        public InputField.LineType lineType
        {
            get { return this.m_LineType; }
            set
            {
                if (this.m_LineType != value)
                {
                    this.m_LineType = value;
                    this.SetToCustomIfContentTypeIsNot(new InputField.ContentType[]
                    {
                        InputField.ContentType.Standard,
                        InputField.ContentType.Autocorrected
                    });
                }
            }
        }
        public bool multiLine
        {
            get { return m_LineType != InputField.LineType.SingleLine; }
        }
        public TouchScreenKeyboardType keyboardType
        {
            get { return m_KeyboardType; }
            set { m_KeyboardType = value; }
        }
        public int characterLimit
        {
            get { return m_CharacterLimit; }
            set { m_CharacterLimit = value; }
        }
        public InputField.CharacterValidation characterValidation
        {
            get { return m_CharacterValidation; }
            set { m_CharacterValidation = value; }
        }
        public char asteriskChar
        {
            get { return this.m_AsteriskChar; }
            set { this.m_AsteriskChar = value; }
        }
        public bool isShowCaret
        {
            get { return this.m_ShowCaret; }
            set { this.m_ShowCaret = value; }
        }
        public float caretBlinkRate
        {
            get { return m_CaretBlinkRate; }
            set { m_CaretBlinkRate = value; }
        }
        //--------------------------------------------------------------------
        public ITextComponent TextComponent
        {
            get { return m_TextComponent; }
            set { m_TextComponent = value; }
        }
        public Graphic Placeholder
        {
            get { return m_Placeholder; }
            set { m_Placeholder = value; }
        }
        //--------------------------------------------------------------------
        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            this.DeactivateInputField();
        }
        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            this.ActivateInputField();
        }
        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            this.ActivateInputField();
        }
        public override void OnPointerDown(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(base.gameObject, eventData);
            bool allowInput = this.m_AllowInput;
            base.OnPointerDown(eventData);
            if (!this.InPlaceEditing() && (this.m_Keyboard == null || !this.m_Keyboard.active))
            {
                this.OnSelect(eventData);
                return;
            }
            this.UpdateLabel();
            eventData.Use();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            this.EnforceContentType();
            if (!this.IsActive())
            {
                return;
            }
            this.UpdateLabel();
        }
#endif
        //--------------------------------------------------------------------
        #region _text_append_

        protected virtual bool Append(char input)
        {
            if (input == '\0')
            {
                return false;
            }
            if (!this.InPlaceEditing())
            {
                return false;
            }
            if (this.characterValidation != InputField.CharacterValidation.None)
            {
                input = this.Validate(this.Text, input);
            }
            if (input == '\0')
            {
                return false;
            }
            string add = input.ToString();
            if (UnityEngine.Application.platform == RuntimePlatform.IPhonePlayer)
            {

            }
            else if (this.characterLimit > 0 && (this.Text.Length + add.Length) >= this.characterLimit)
            {
                return false;
            }
            this.m_Text += add;
            this.SendOnValueChanged();
            return true;
        }
        protected virtual bool DeleteAll()
        {
            if (m_Text.Length > 0)
            {
                this.m_Text = string.Empty;
                this.SendOnValueChanged();
                return true;
            }
            return false;
        }
        protected virtual bool Delete()
        {
            if (m_Text.Length > 0)
            {
                this.m_Text = m_Text.Substring(0, m_Text.Length - 1);
                this.SendOnValueChanged();
                return true;
            }
            return false;
        }
        protected virtual bool IsValidChar(char c)
        {
            return c != '\u007f';
        }

        protected char Validate(string text, char ch)
        {
            int pos = text.Length;
            if (this.characterValidation == InputField.CharacterValidation.None || !base.enabled)
            {
                return ch;
            }
            if (this.characterValidation == InputField.CharacterValidation.Integer || this.characterValidation == InputField.CharacterValidation.Decimal)
            {
                if (pos != 0 || text.Length <= 0 || text[0] != '-')
                {
                    if (ch >= '0' && ch <= '9')
                    {
                        return ch;
                    }
                    if (ch == '-' && pos == 0)
                    {
                        return ch;
                    }
                    if (ch == '.' && this.characterValidation == InputField.CharacterValidation.Decimal && !text.Contains("."))
                    {
                        return ch;
                    }
                }
            }
            else
            {
                if (this.characterValidation == InputField.CharacterValidation.Alphanumeric)
                {
                    if (ch >= 'A' && ch <= 'Z')
                    {
                        return ch;
                    }
                    if (ch >= 'a' && ch <= 'z')
                    {
                        return ch;
                    }
                    if (ch >= '0' && ch <= '9')
                    {
                        return ch;
                    }
                }
                else
                {
                    if (this.characterValidation == InputField.CharacterValidation.Name)
                    {
                        char c = (text.Length <= 0) ? ' ' : text[Mathf.Clamp(pos, 0, text.Length - 1)];
                        char c2 = (text.Length <= 0) ? '\n' : text[Mathf.Clamp(pos + 1, 0, text.Length - 1)];
                        if (char.IsLetter(ch))
                        {
                            if (char.IsLower(ch) && c == ' ')
                            {
                                return char.ToUpper(ch);
                            }
                            if (char.IsUpper(ch) && c != ' ' && c != '\'')
                            {
                                return char.ToLower(ch);
                            }
                            return ch;
                        }
                        else
                        {
                            if (ch == '\'')
                            {
                                if (c != ' ' && c != '\'' && c2 != '\'' && !text.Contains("'"))
                                {
                                    return ch;
                                }
                            }
                            else
                            {
                                if (ch == ' ' && c != ' ' && c != '\'' && c2 != ' ' && c2 != '\'')
                                {
                                    return ch;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (this.characterValidation == InputField.CharacterValidation.EmailAddress)
                        {
                            if (ch >= 'A' && ch <= 'Z')
                            {
                                return ch;
                            }
                            if (ch >= 'a' && ch <= 'z')
                            {
                                return ch;
                            }
                            if (ch >= '0' && ch <= '9')
                            {
                                return ch;
                            }
                            if (ch == '@' && text.IndexOf('@') == -1)
                            {
                                return ch;
                            }
                            if ("!#$%&'*+-/=?^_`{|}~".IndexOf(ch) != -1)
                            {
                                return ch;
                            }
                            if (ch == '.')
                            {
                                char c3 = (text.Length <= 0) ? ' ' : text[Mathf.Clamp(pos, 0, text.Length - 1)];
                                char c4 = (text.Length <= 0) ? '\n' : text[Mathf.Clamp(pos + 1, 0, text.Length - 1)];
                                if (c3 != '.' && c4 != '.')
                                {
                                    return ch;
                                }
                            }
                        }
                    }
                }
            }
            return '\0';
        }

        protected void UpdateLabel()
        {
            if (this.m_TextComponent != null)
            {
                string text = this.Text;
                string text2 = this.Text;
                if (this.inputType == InputField.InputType.Password)
                {
                    text2 = new string(this.asteriskChar, text.Length);
                }
                bool flag = string.IsNullOrEmpty(text);
                if (this.m_Placeholder != null)
                {
                    this.m_Placeholder.enabled = flag;
                }
                this.m_TextComponent.Text = text2;
            }
        }
        //--------------------------------------------------------------------
        private void SendOnValueChangedAndUpdateLabel()
        {
            this.SendOnValueChanged();
            this.UpdateLabel();
        }
        private void SendOnValueChanged()
        {
            if (this.event_ValueChanged != null)
            {
                this.event_ValueChanged.Invoke(this.Text);
            }
        }
        protected void SendOnSubmit()
        {
            if (UnityEngine.Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (this.characterLimit > 0 && this.m_Text.Length > this.characterLimit)
                {
                    this.m_Text = this.m_Text.Substring(0, this.characterLimit);
                }
            }
            if (this.event_EndEdit != null)
            {
                this.event_EndEdit.Invoke(this.m_Text);
            }
        }
        private void EnforceContentType()
        {
            switch (this.contentType)
            {
                case InputField.ContentType.Standard:
                    this.m_InputType = InputField.InputType.Standard;
                    this.m_KeyboardType = TouchScreenKeyboardType.Default;
                    this.m_CharacterValidation = InputField.CharacterValidation.None;
                    return;
                case InputField.ContentType.Autocorrected:
                    this.m_InputType = InputField.InputType.AutoCorrect;
                    this.m_KeyboardType = TouchScreenKeyboardType.Default;
                    this.m_CharacterValidation = InputField.CharacterValidation.None;
                    return;
                case InputField.ContentType.IntegerNumber:
                    this.m_LineType = InputField.LineType.SingleLine;
                    this.m_InputType = InputField.InputType.Standard;
                    this.m_KeyboardType = TouchScreenKeyboardType.NumberPad;
                    this.m_CharacterValidation = InputField.CharacterValidation.Integer;
                    return;
                case InputField.ContentType.DecimalNumber:
                    this.m_LineType = InputField.LineType.SingleLine;
                    this.m_InputType = InputField.InputType.Standard;
                    this.m_KeyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
                    this.m_CharacterValidation = InputField.CharacterValidation.Decimal;
                    return;
                case InputField.ContentType.Alphanumeric:
                    this.m_LineType = InputField.LineType.SingleLine;
                    this.m_InputType = InputField.InputType.Standard;
                    this.m_KeyboardType = TouchScreenKeyboardType.ASCIICapable;
                    this.m_CharacterValidation = InputField.CharacterValidation.Alphanumeric;
                    return;
                case InputField.ContentType.Name:
                    this.m_LineType = InputField.LineType.SingleLine;
                    this.m_InputType = InputField.InputType.Standard;
                    this.m_KeyboardType = TouchScreenKeyboardType.Default;
                    this.m_CharacterValidation = InputField.CharacterValidation.Name;
                    return;
                case InputField.ContentType.EmailAddress:
                    this.m_LineType = InputField.LineType.SingleLine;
                    this.m_InputType = InputField.InputType.Standard;
                    this.m_KeyboardType = TouchScreenKeyboardType.EmailAddress;
                    this.m_CharacterValidation = InputField.CharacterValidation.EmailAddress;
                    return;
                case InputField.ContentType.Password:
                    this.m_LineType = InputField.LineType.SingleLine;
                    this.m_InputType = InputField.InputType.Password;
                    this.m_KeyboardType = TouchScreenKeyboardType.Default;
                    this.m_CharacterValidation = InputField.CharacterValidation.None;
                    return;
                case InputField.ContentType.Pin:
                    this.m_LineType = InputField.LineType.SingleLine;
                    this.m_InputType = InputField.InputType.Password;
                    this.m_KeyboardType = TouchScreenKeyboardType.NumberPad;
                    this.m_CharacterValidation = InputField.CharacterValidation.Integer;
                    return;
                default:
                    return;
            }
        }
        private void SetToCustomIfContentTypeIsNot(params InputField.ContentType[] allowedContentTypes)
        {
            if (this.contentType == InputField.ContentType.Custom)
            {
                return;
            }
            for (int i = 0; i < allowedContentTypes.Length; i++)
            {
                if (this.contentType == allowedContentTypes[i])
                {
                    return;
                }
            }
            this.contentType = InputField.ContentType.Custom;
        }
        private void SetToCustom()
        {
            if (this.contentType == InputField.ContentType.Custom)
            {
                return;
            }
            this.contentType = InputField.ContentType.Custom;
        }

        #endregion
        //--------------------------------------------------------------------
        #region _keyboard_

        private bool InPlaceEditing()
        {
            return !TouchScreenKeyboard.isSupported;
        }

        public void ProcessEvent(Event e)
        {
            this.KeyPressed(e);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <returns>finish</returns>
        protected bool KeyPressed(Event evt)
        {
            KeyCode keyCode = evt.keyCode;
            switch (evt.keyCode)
            {
                case KeyCode.KeypadEnter:
                    if (this.lineType != InputField.LineType.MultiLineNewline)
                    {
                        return true;
                    }
                    break;
                case KeyCode.Delete:
                    if (DeleteAll())
                    {
                        this.UpdateLabel();
                    }
                    return false;
                case KeyCode.Backspace:
                    if (this.Delete())
                    {
                        this.UpdateLabel();
                    }
                    return false;
            }
            char c = evt.character;
            if (!this.multiLine && (c == '\t' || c == '\r' || c == '\n'))
            {
                return false;
            }
            if (c == '\r' || c == '\u0003')
            {
                c = '\n';
            }
            if (this.IsValidChar(c))
            {
                this.Append(c);
                this.UpdateLabel();
            }
            return false;
        }
        public virtual void OnUpdateSelected(BaseEventData eventData)
        {
            if (!this.isFocused)
            {
                return;
            }
            while (Event.PopEvent(this.m_ProcessingEvent))
            {
                if (this.m_ProcessingEvent.rawType == EventType.KeyDown)
                {
                    bool finish = this.KeyPressed(this.m_ProcessingEvent);
                    if (finish)
                    {
                        this.DeactivateInputField();
                        break;
                    }
                }
            }
            eventData.Use();
        }



        protected virtual void OnTouchKeoboardFinish(TouchScreenKeyboard keyboard)
        {
            string text = keyboard.text;
            if (this.m_Text != text)
            {
                this.m_Text = string.Empty;
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    if (c == '\r' || c == '\u0003')
                    {
                        c = '\n';
                    }
                    if (this.characterValidation != InputField.CharacterValidation.None)
                    {
                        c = this.Validate(this.m_Text, c);
                    }
                    if (c == '\n')
                    {
                        keyboard.text = this.m_Text;
                        this.OnDeselect(null);
                        return;
                    }
                    if (c != '\0')
                    {
                        this.m_Text += c;
                    }
                }
                if (UnityEngine.Application.platform == RuntimePlatform.IPhonePlayer)
                {

                }
                else if (this.characterLimit > 0 && this.m_Text.Length > this.characterLimit)
                {
                    this.m_Text = this.m_Text.Substring(0, this.characterLimit);
                }
                int length = this.m_Text.Length;
                if (this.m_Text != text)
                {
                    keyboard.text = this.m_Text;
                }
                this.SendOnValueChangedAndUpdateLabel();
            }
        }
        //--------------------------------------------------------------------
        public void ActivateInputField()
        {
            if (this.m_TextComponent == null || !this.IsActive() || !this.IsInteractable())
            {
                return;
            }
            if (this.m_Keyboard != null && !this.m_Keyboard.active)
            {
                this.m_Keyboard.active = true;
                this.m_Keyboard.text = this.m_Text;
            }
            this.m_ShouldActivateNextUpdate = true;
        }
        private void ActivateInputFieldInternal()
        {
            if (EventSystem.current.currentSelectedGameObject != base.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(base.gameObject);
            }
            if (TouchScreenKeyboard.isSupported)
            {
                this.m_Keyboard = TouchScreenKeyboard.Open(
                    this.m_Text,
                    this.keyboardType,
                    this.inputType == InputField.InputType.AutoCorrect,
                    this.multiLine,
                    this.inputType == InputField.InputType.Password);
            }
            else
            {
                Input.imeCompositionMode = IMECompositionMode.On;
            }
            this.m_OriginalText = this.Text;
            this.m_WasCanceled = false;
            this.m_AllowInput = true;
        }
        public void DeactivateInputField()
        {
            if (!this.m_AllowInput)
            {
                return;
            }
            this.m_AllowInput = false;
            if (this.m_TextComponent != null && this.IsInteractable())
            {
                if (this.m_WasCanceled)
                {
                    this.Text = this.m_OriginalText;
                }
                if (this.m_Keyboard != null)
                {
                    this.m_Keyboard.active = false;
                    this.m_Keyboard = null;
                }
                this.SendOnSubmit();
                Input.imeCompositionMode = IMECompositionMode.Auto;
            }
        }
        #endregion
        //--------------------------------------------------------------------

        protected virtual void LateUpdate()
        {
            if (this.m_ShouldActivateNextUpdate)
            {
                if (!this.isFocused)
                {
                    this.ActivateInputFieldInternal();
                    this.m_ShouldActivateNextUpdate = false;
                    return;
                }
                this.m_ShouldActivateNextUpdate = false;
            }
            UpdateCaret();
            if (this.InPlaceEditing() || !this.isFocused)
            {
                return;
            }
            if (this.m_Keyboard == null || !this.m_Keyboard.active)
            {
                if (this.m_Keyboard != null && this.m_Keyboard.wasCanceled)
                {
                    this.m_WasCanceled = true;
                }
                this.OnDeselect(null);
                return;
            }
            this.OnTouchKeoboardFinish(m_Keyboard);
            if (this.m_Keyboard.done)
            {
                if (this.m_Keyboard.wasCanceled)
                {
                    this.m_WasCanceled = true;
                }
                this.OnDeselect(null);
            }
        }

        //--------------------------------------------------------------------
        public void GraphicUpdateComplete()
        {
        }
        public void LayoutComplete()
        {
        }
        public void OnSubmit(BaseEventData eventData)
        {
        }
        public void Rebuild(CanvasUpdate executing)
        {
        }
        //--------------------------------------------------------------------
        private void UpdateCaret()
        {
            if (isShowCaret && isFocused)
            {
                if (m_TextComponent != null)
                {
                    if (m_CaretSprite == null)
                    {
                        m_CaretSprite = new CaretSprite();
                        Binding.AddChild(m_CaretSprite);
                    }
                    var cb = m_TextComponent.LastCaretPosition;
                    cb.x += m_TextComponent.Binding.X;
                    cb.y += m_TextComponent.Binding.Y;
                    m_CaretSprite.Bounds2D = cb;
                    m_CaretSprite.Visible = (cb.width > 0);
                    m_CaretSprite.UpdateCaret(m_CaretBlinkRate);
                }
            }
            else
            {
                if (m_CaretSprite != null)
                {
                    m_CaretSprite.Visible = false;
                }
            }
        }
        private class CaretSprite : TintSprite
        {
            private float m_CaretSEC;
            private int m_CaretTimer;

            internal CaretSprite() : base("caret") { }
            internal bool UpdateCaret(float blinkRate)
            {
                var delta = UnityEngine.Time.deltaTime;
                m_CaretSEC += delta;
                if (m_CaretSEC > blinkRate)
                {
                    m_CaretSEC = m_CaretSEC % blinkRate;
                    m_CaretTimer++;
                }
                this.VisibleInParent = (m_CaretTimer % 2 == 0);
                return this.VisibleInParent;
            }
        }
    }



}
