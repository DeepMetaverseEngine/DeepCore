using DeepCore.GUI.Cell;
using UnityEngine;

namespace DeepCore.Unity3D.UGUI
{
    public partial class ImageFontSprite : DisplayText
    {
        private ImageFontGraphics mGraphics;

        public ImageFontSprite(string name = "") : base(name)
        {
            this.Enable = false;
            this.EnableChildren = false;
            this.mGraphics = mGameObject.AddComponent<ImageFontGraphics>();
        }
        public ImageFontGraphics Graphics
        {
            get { return mGraphics; }
        }

        public override string Text
        {
            get { return mGraphics.Text; }
            set
            {
                if (IsDispose) return;
                this.mGraphics.Text = value;
            }
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
            get { return 1; }
            set { }
        }
        public override GUI.Data.FontStyle Style
        {
            get { return GUI.Data.FontStyle.Normal; }
            set { }
        }
        public override bool IsUnderline
        {
            get { return false; }
            set { }
        }

        public override Vector2 PreferredSize
        {
            get { return mGraphics.PreferredSize; }
        }
        public override Rect LastCaretPosition
        {
            get { return mGraphics.LastCaretPosition; }
        }

        public void SetAtlas(CPJAtlas atlas)
        {
            this.mGraphics.Atlas = atlas;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
        }

        public override void SetBorder(Color bc, Vector2 distance)
        {
            //Do nothing.
        }
        public override void SetShadow(Color bc, Vector2 distance)
        {

        }
        public override void SetFont(UnityEngine.Font font)
        {

        }
    }

}
