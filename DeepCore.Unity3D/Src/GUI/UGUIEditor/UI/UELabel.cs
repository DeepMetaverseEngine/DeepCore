using DeepCore.GUI.Cell;
using DeepCore.Unity3D.UGUI;
using DeepCore.GUI.Data;

namespace DeepCore.Unity3D.UGUIEditor.UI
{
    public partial class UELabel : UETextComponent
    {
        public UELabel() : this(UIEditor.GlobalUseBitmapText)
        {

        }
        public UELabel(bool use_bitmap) : base(use_bitmap)
        {
            this.Enable = false;
            this.EnableChildren = false;
        }

        public UELabel(string text, UILayout layout = null, CPJAtlas imageFont = null, bool use_bitmap = false) : base(use_bitmap)
        {
            this.Enable = false;
            this.EnableChildren = false;

            if (layout != null)
            {
                if (imageFont != null)
                {
                    var vt = new ImageFontSprite("image_font");
                    vt.SetAtlas(imageFont);
                    this.mTextSprite = vt;
                }
                else if (base.mUseBitmapFont)
                {
                    mTextSprite = new BitmapTextSprite("bitmap_text");
                }
                else
                {
                    mTextSprite = new TextSprite("text");
                }
                this.AddChild(mTextSprite);
            }
            else
            {
                if (imageFont != null)
                {
                    var vt = mGameObject.AddComponent<ImageFontGraphics>();
                    this.mTextGraphics = vt;
                    vt.Atlas = imageFont;
                }
                else if (base.mUseBitmapFont)
                {
                    this.mTextGraphics = mGameObject.AddComponent<BitmapTextGraphics>();
                }
                else
                {
                    this.mTextGraphics = mGameObject.AddComponent<TextGraphics>().DefaultInit();
                }
            }
            this.Text = text;
        }

        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);

            if(UIEditorMeta.UIEditorRunTime)
            {
                this.EnableChildren = e.EnableChilds;
                this.Enable = e.Enable;
                this.IsInteractive = true;
            }
        }
    }

}
