using System;
using UnityEngine;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{
    public interface IInputField : IInteractiveComponent
    {
        string Text { get; set; }

        InputField.InputType inputType { get; set; }
        InputField.ContentType contentType { get; set; }
        InputField.LineType lineType { get; set; }
        InputField.CharacterValidation characterValidation { get; set; }
        TouchScreenKeyboardType keyboardType { get; set; }
        int characterLimit { get; set; }
        char asteriskChar { get; set; }
        float caretBlinkRate { get; set; }

        bool isFocused { get; }
        bool multiLine { get; }

        Action<string> event_EndEdit { get; set; }
        Action<string> event_ValueChanged { get; set; }
    }
    


}
