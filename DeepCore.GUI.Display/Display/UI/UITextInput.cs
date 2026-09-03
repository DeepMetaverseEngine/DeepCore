using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;

namespace DeepCore.GUI.Display.UI
{
    public class UITextInput : UIComponent
    {
        private TRectangle2D rect;
        public enum KeyBoardType
        {
            Default,//默认为当前输入法的类型.
            ASCIICapable,//显示一个键盘可以输入ASCII字符，非ASCII键盘保持活跃.
            NumbersAndPunctuation,//数字和各种标点符号.
            Url,//URL条目进行了优化.
            NumberPad,//数字键盘（0-9）.
            PhonePad,//一个电话垫（1-9，*，0，＃，字母下的数字).
            NamePhonePad,//姓名或电话号码.
            EmailAddress,//多个电子邮件地址输入.
            DecimalPad,//一个带小数点的数字键盘.
            Twitter//一个Twitter的文本输入优化.
        }

        private KeyBoardType mType = KeyBoardType.Default;
        private UITextField mTextField;
        private UITextBox mTextBox;
        private Anchor mAnchor = Anchor.ANCHOR_LEFT | Anchor.ANCHOR_VCENTER;
        private int mMaxLength = 100;
        private bool mTextAsPassWord = false;
        private const int FADEIN = 1;
        private const int FADEOUT = 2;
        private int mFadeStatus = 0;
        private int mFrameCount = 0;
        private uint mFrameFrequency = 2;
        public delegate char Validator(string currentText, char nextChar);
        public Validator OnValidator;

        public delegate void InputFinishCallBack(string rlt);
        public InputFinishCallBack OnInputFinishCallBack;

        private bool mMultipleLine = false;
        private DisplayNode alphaNode = null;
        public UITextInput()
        {
            this.Enable = true;
            this.EnableChildren = false;
            mTextBox = CreateTextBox("", UIEditor.Instance.DefaultFontSize, UIEditor.Instance.DefaultFontStyle);
            mTextField = CreateTextField("", UIEditor.Instance.DefaultFontSize, UIEditor.Instance.DefaultFontStyle);
            VisibleControl();
        }

        public string Text
        {
            set
            {
                if(value == null)
                {
                    return;
                }
                if(mMultipleLine == true)
                {
                    if(mTextBox == null)
                    {
                        mTextBox = CreateTextBox(value, UIEditor.Instance.DefaultFontSize, UIEditor.Instance.DefaultFontStyle);
                        this.AddChild(mTextBox);
                    }

                    if(mTextBox != null)
                    {
                        mTextBox.Text = value;
                    }

                }
                else
                {
                    if(mTextField == null)
                    {
                        mTextField = CreateTextField(value, UIEditor.Instance.DefaultFontSize, UIEditor.Instance.DefaultFontStyle);
                        if(mTextField != null)
                        {
                            this.AddChild(mTextField);
                            ResetTextField(mTextField, Width, Height);
                        }
                    }

                    if(mTextField != null)
                    {
                        mTextField.Text = value;
                        float w = mTextField.GetTextWidth();
                        if(w > Width)//文字内容靠右显示.
                        {
                            mTextField.X = Width - w;
                        }
                        else 
                        {
                            ResetTextField(mTextField, Width, Height);                        
                        }
                    }
                }

            }

            get
            {
                if(mMultipleLine == true)
                {
                    if(mTextBox == null)
                    {
                        return "";
                    }
                    return mTextBox.Text;
                }
                else
                {
                    if(mTextField == null)
                    {
                        return "";
                    }
                    return mTextField.Text;
                }

            }
        }

        public virtual bool TextAsPassWord
        {
            set
            {
                mTextAsPassWord = value;
            }
            get
            {
                return mTextAsPassWord;
            }
        }

        public int MaxLength
        {
            get
            {
                return mMaxLength;
            }
            set
            {
                mMaxLength = value;
            }
        }

        public KeyBoardType InputType
        {
            set
            {
                mType = value;
            }
            get
            {
                return mType;
            }
        }

        public bool MutlipleLine
        {
            set
            {
                mMultipleLine = value;
                VisibleControl();
            }
            get
            {
                return mMultipleLine;
            }
        }

        private void VisibleControl()
        {
            if(mMultipleLine == false)
            {
                this.AddChild(mTextField);
                if(mTextBox != null)
                {
                    mTextBox.RemoveFromParent(false);
                }
            }
            else
            {
                if(mTextBox != null)
                {
                    this.AddChild(mTextBox);
                }
                mTextField.RemoveFromParent(false);
            }
        }

        private UITextField CreateTextField(string text, float fontSize, FontStyle style = FontStyle.STYLE_PLAIN)
        {
            return new UITextField(text, fontSize, style);
        }

        private UITextBox CreateTextBox(string text, float fontSize, FontStyle style = FontStyle.STYLE_PLAIN)
        {
            UITextBox textBox = new UITextBox();
            textBox.SetFontSize(fontSize);
            textBox.Text = text;
            textBox.SetTextColor(0xffffffff);
            textBox.RichTextLayer.SetWidth(this.Width);
            return textBox;
        }

        private void ResetTextField(UITextField o, float w, float h)
        {
            if(o == null)
            {
                return;
            }

            if((mAnchor & Anchor.ANCHOR_HCENTER) != 0)
            {
                o.X = w * 0.5f;
            }
            else if((mAnchor & Anchor.ANCHOR_RIGHT) != 0)
            {
                o.X = w;
            }
            else
            {
                o.X = 0;
            }


            if((mAnchor & Anchor.ANCHOR_VCENTER) != 0)
            {
                o.Y = h * 0.5f;
            }
            else if((mAnchor & Anchor.ANCHOR_BOTTOM) != 0)
            {
                o.Y = h;
            }
            else
            {
                o.Y = 0;
            }

        }

        /// <summary>
        /// 设置输入框内文字的大小.
        /// </summary>
        /// <param name="size"></param>
        public void SetTextSize(int size)
        {
            if(mTextField == null)
            {
                mTextField = CreateTextField(" ", size, UIEditor.Instance.DefaultFontStyle);
                if(mTextField != null)
                {
                    this.AddChild(mTextField);
                }
            }

            if(mTextField != null)
            {
                mTextField.SetFontSize(size);
            }

            if(mTextBox != null)
            {
                mTextBox.SetFontSize(size);
            }

        }

        /// <summary>
        /// 设置输入框内文字锚点.
        /// </summary>
        /// <param name="anchor"></param>
        public void SetTextAnchor(Anchor anchor)
        {
            mAnchor = anchor;

            if(mTextField == null)
            {
                mTextField = CreateTextField(" ", UIEditor.Instance.DefaultFontSize, UIEditor.Instance.DefaultFontStyle);
                if(mTextField != null)
                {
                    this.AddChild(mTextField);
                }
            }

            if(mTextField != null)
            {
                mTextField.SetAnchor(anchor);
                ResetTextField(mTextField, Width, Height);
            }

        }

        /// <summary>
        /// 设置限定尺寸，内容超过该尺寸后，自动换行.
        /// </summary>
        /// <param name="size"></param>
        public void SetExpectSize(Size2D size)
        {

            if(mTextField != null)
            {
                //mTextField.SetExceptSize(size);
            }

            if(mTextBox != null)
            {
                mTextBox.SetExceptSize(size);
            }
        }

        /// <summary>
        /// 设置输入框文字颜色.
        /// </summary>
        /// <param name="rgba"></param>
        public void SetFontColor(uint rgba)
        {
            if(mTextBox != null)
            {
                mTextBox.SetTextColor(rgba);
            }
            if(mTextField != null)
            {
                mTextField.SetFontColor(rgba);
            }
        }

        public void SetBorderTimes(int times,uint corlor)
        {
            if(mTextBox != null)
            {
                mTextBox.RichTextLayer.SetBorder(times,corlor);
            }
            if(mTextField != null)
            {
                mTextField.SetBorderTimes(times);
                mTextField.SetBorderColor(corlor);
            }
        }

        protected override void Disposing()
        {
            Driver.Instance.CloseIME();
            
            if(mTextField != null)
            {
                mTextField.RemoveFromParent(false);
                mTextField.Dispose();
                mTextField = null;
            }

            if(mTextBox != null)
            {
                mTextBox.RemoveFromParent(false);
                mTextBox.Dispose();
                mTextBox = null;
            }
        
            alphaNode = null;
            OnValidator = null;
            OnInputFinishCallBack = null;
            base.Disposing();
        }

        public override void TouchClick(NodeTouch touch)
        {
            base.TouchClick(touch);
            Driver.Instance.OpenIME(this);
        }

        public override void TouchOut(NodeTouch touch)
        {
            Driver.Instance.CloseIME();
            base.TouchOut(touch);
        }

        /// <summary>
        /// 显示文字.
        /// </summary>
        /// <param name="value"></param>
        public void ShowText(bool value)
        {
            mTextBox.Visible = value;
            mTextField.Visible = value;
        }

        /// <summary>
        /// 输入模式下alpha渐变.
        /// </summary>
        /// <param name="isStart"></param>
        public void FadeTextField(bool isStart)
        {
            if(mMultipleLine == true)
            {
                alphaNode = mTextBox;
            }
            else
            {
                alphaNode = mTextField;
            }



            if(isStart == true)
            {
                mFadeStatus = FADEOUT;
            }
            else
            {
                mFadeStatus = 0;
                alphaNode.Alpha = 1;
            }
        }

        /// <summary>
        /// 设置alpha渐变每多少帧执行一次.
        /// </summary>
        /// <param name="frame"></param>
        public void SetAlphaFadeFrameFrequency(uint frame)
        {
            mFrameFrequency = frame;
        }

        //IME输入结束回调.
        public void SetInputFinish(string text)
        {
            if(OnInputFinishCallBack != null)
            {
                OnInputFinishCallBack(text);
            }
        }

        /// <summary>
        /// 控件输入的字符，做验证，如果不符合验证规则则将当前字符重新处理.
        /// </summary>
        /// <param name="currentText"></param>
        /// <param name="nextChar"></param>
        /// <returns></returns>
        public char DoValidator(string currentText, char nextChar)
        {
            char rlt = nextChar;

            rlt = FilterCharByType(nextChar);

            if(OnValidator != null)
            {
                rlt = OnValidator(currentText, rlt);
            }
            return rlt;
        }

        private char FilterCharByType(char c)
        {
            switch(mType)
            {
                case KeyBoardType.NumberPad:
                    if(!char.IsNumber(c))
                    {
                        c = '\0';
                    }
                    break;
                default:
                    break;
            }

            return c;
        }

        public override void Update(float delatTime)
        {
            base.Update(delatTime);

            if (alphaNode == null)
            {
                return;
            }

            mFrameCount++;

            if(mFrameCount < mFrameFrequency)
            {
                return;
            }
            mFrameCount = 0;

            switch(mFadeStatus)
            {
                case FADEOUT:
                    alphaNode.Alpha -= 0.1f;
                    if(alphaNode.Alpha == 0.0f)
                    {
                        mFadeStatus = FADEIN;
                    }
                    break;
                case FADEIN:
                    alphaNode.Alpha += 0.1f;
                    if(alphaNode.Alpha == 1.0f)
                    {
                        mFadeStatus = FADEOUT;
                    }
                    break;
                default:
                    break;
            }
        }

        public override void Visit(Display.Graphics g)
        {
            if(ScrollRect != null)
            {
                g.PushClip();
                //渲染优化，使用结构体避免不必要的内存开销.
                rect = this.LocalToGlobal_S(ScrollRect);
                g.SetClip(rect.x, rect.y, rect.width, rect.height);
                base.Visit(g);
                g.PopClip();
            }
            else
            {
                base.Visit(g);
            }
        }

        protected override void DecodeFields(UIEditor editor, Data.UIComponentMeta e)
        {
            base.DecodeFields(editor, e);

            if (e is Data.UETextInputBaseMeta)
            {
                Data.UETextInputBaseMeta meta = e as Data.UETextInputBaseMeta;

                this.Text = meta.Text;
                this.TextAsPassWord = meta.isPassword;
                this.SetTextSize(meta.textFontSize);
                this.SetFontColor(Color.toRGBA(meta.textColor));
                this.SetExpectSize(new Gemo.Size2D(Width, Height));
                this.SetTextAnchor(Anchor.ANCHOR_LEFT | Anchor.ANCHOR_VCENTER);

            }

        }
    }
}
