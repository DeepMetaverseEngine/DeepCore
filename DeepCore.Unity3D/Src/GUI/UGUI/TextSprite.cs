using UnityEngine;


namespace DeepCore.Unity3D.UGUI
{
    /// <summary>
    /// 显示单行文本或简单文本
    /// </summary>
    public partial class TextSprite : DisplayText
    {
        private readonly TextGraphics mGraphics;

        public TextSprite(string name = "") : base(name)
        {
            this.mGraphics = mGameObject.AddComponent<TextGraphics>().DefaultInit();
            this.Enable = false;
            this.EnableChildren = false;
        }
        public TextGraphics Graphics
        {
            get { return mGraphics; }
        }
        public override string Text
        {
            get { return mGraphics.text; }
            set
            {
                if (IsDispose) return;
                mGraphics.text = value;
            }
        }
        public override int FontSize
        {
            get { return mGraphics.fontSize; }
            set { mGraphics.fontSize = value; }
        }
        public override UnityEngine.Color FontColor
        {
            get { return mGraphics.FontColor; }
            set { mGraphics.FontColor = value; }
        }
        public override GUI.Data.TextAnchor Anchor
        {
            get { return mGraphics.Anchor; }
            set { this.mGraphics.Anchor = value; }
        }
        public override Vector2 TextOffset
        {
            get { return mGraphics.TextOffset; }
            set { this.mGraphics.TextOffset = value; }
        }
        public override GUI.Data.FontStyle Style
        {
            get { return (GUI.Data.FontStyle)mGraphics.fontStyle; }
            set { this.mGraphics.fontStyle = (UnityEngine.FontStyle)value; }
        }
        public override bool IsUnderline
        {
            get { return mGraphics.IsUnderline; }
            set { this.mGraphics.IsUnderline = value; }
        }

        public override Vector2 PreferredSize
        {
            get { return mGraphics.PreferredSize; }
        }
        public override Rect LastCaretPosition
        {
            get { return mGraphics.LastCaretPosition; }
        }



        public UnityEngine.UI.Outline AddBorder(UnityEngine.Color bc, Vector2 distance)
        {
            return mGraphics.AddBorder(bc, distance);
        }
        public UnityEngine.UI.Shadow AddShadow(UnityEngine.Color bc, Vector2 distance)
        {
            return mGraphics.AddShadow(bc, distance);
        }
        public void SetTextFont(UnityEngine.Font font, int size, UnityEngine.FontStyle style)
        {
            mGraphics.SetTextFont(font, size, style);
        }

        public override void SetBorder(Color bc, Vector2 distance)
        {
            AddBorder(bc, distance);
        }
        public override void SetShadow(Color bc, Vector2 distance)
        {
            AddShadow(bc, distance);
        }
        public override void SetFont(Font font)
        {
            mGraphics.font = font;
        }

    }

    //---------------------------------------------------------------------------------------------------
}
