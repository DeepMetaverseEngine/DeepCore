#if HZUI
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DeepCore.GUI.UI;
using DeepCore.GUI.Gemo;

namespace DeepCore.Unity3D_IOS
{
    public class UnityPlatformIOSTextInput : MonoBehaviour
    {
        private UITextInput mInput = null;
        private int mMaxLength = 100;
        private string mText = "";
        protected TouchScreenKeyboard mKeyboard;

        public void SetInput(UITextInput input)
        {
            //if input = null then seems to close KeyBoard.
            if (input == null && mInput != null)
            {
                mInput.FadeTextField(false);
                mInput.Text = mText;
                mInput.SetInputFinish(mText);
                if (mKeyboard != null) { mKeyboard.active = false; }
            }

            mInput = input;

            //open KeyBoard.
            if (mInput != null && !mInput.IsDispose)
            {
                mInput.FadeTextField(true);
                mMaxLength = input.MaxLength;
                mText = mInput.Text;
                TouchScreenKeyboard.hideInput = false;
                mKeyboard = TouchScreenKeyboard.Open(mText, ConvertKeyBoardType(mInput.InputType), false);
            }
        }

        void Update()
        {
            if (mKeyboard != null)
            {
                //update Content.
                string text = mKeyboard.text;
                
                if(text == null)
                {
                    text = "";
                }

                if(mText != text && mInput != null && !mInput.IsDispose)
                {
                    mText = "";

                    for (int i = 0; i < text.Length; ++i)
                    {
                        char ch = text[i];
                        ch = mInput.DoValidator(mText, ch);
                        if (ch != 0) mText += ch;
                    }

                    if (mMaxLength > 0 && mText.Length > mMaxLength) mText = mText.Substring(0, mMaxLength);
                    if (mText != text) mKeyboard.text = mText;
                    UpdateInputText();
                }

                //check input status.
                if (mKeyboard.done || !mKeyboard.active)
                {
                    mKeyboard = null;
                    this.SetInput(null);
                }

            }
        }

        private TouchScreenKeyboardType ConvertKeyBoardType(UITextInput.KeyBoardType type)
        {
            TouchScreenKeyboardType rlt = TouchScreenKeyboardType.Default;

            switch (type)
            {
                case UITextInput.KeyBoardType.NumberPad:
                    rlt = TouchScreenKeyboardType.NumberPad;
                    break;
                case UITextInput.KeyBoardType.PhonePad:
                    rlt = TouchScreenKeyboardType.PhonePad;
                    break;
                case UITextInput.KeyBoardType.EmailAddress:
                    rlt = TouchScreenKeyboardType.EmailAddress;
                    break;
                default: break;
            }

            return rlt;
        }

        private void UpdateInputText()
        {
            mInput.Text = mText;
        }
    }
}

#endif
