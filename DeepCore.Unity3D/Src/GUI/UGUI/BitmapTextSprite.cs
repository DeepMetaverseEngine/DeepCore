using UnityEngine;

namespace DeepCore.Unity3D.UGUI
{
    public partial class BitmapTextSprite : DisplayText
    {
        private readonly BitmapTextGraphics mGraphics;

        public BitmapTextSprite(string name = "") : base(name)
        {
            this.mGraphics = mGameObject.AddComponent<BitmapTextGraphics>();
            this.Enable = false;
            this.EnableChildren = false;
        }
        public BitmapTextGraphics Graphics
        {
            get { return mGraphics; }
        }
        public virtual bool AutoScrollToCaret
        {
            get { return mGraphics.AutoScrollToCaret; }
            set { mGraphics.AutoScrollToCaret = value; }
        }
        public override string Text
        {
            get { return this.mGraphics.Text; }
            set
            {
                if (IsDispose) return;
                this.mGraphics.Text = value; }
        }
        public override GUI.Data.TextAnchor Anchor
        {
            get { return mGraphics.Anchor; }
            set { this.mGraphics.Anchor = value; }
        }
        public override Color FontColor
        {
            get { return mGraphics.FontColor; }
            set { mGraphics.FontColor = value; }
        }
        public override Vector2 TextOffset
        {
            get { return mGraphics.TextOffset; }
            set { this.mGraphics.TextOffset = value; }
        }
        public override int FontSize
        {
            get { return mGraphics.FontSize; }
            set { this.mGraphics.FontSize = value; }
        }
        public override GUI.Data.FontStyle Style
        {
            get { return mGraphics.Style; }
            set { this.mGraphics.Style = value; }
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

        public override void SetBorder(Color bc, Vector2 distance)
        {
            mGraphics.SetBorder(bc, distance);
        }
        public override void SetShadow(Color bc, Vector2 distance)
        {
            mGraphics.SetShadow(bc, distance);
        }
        public override void SetFont(Font font)
        {
            mGraphics.SetFont(font);
        }
    }
}